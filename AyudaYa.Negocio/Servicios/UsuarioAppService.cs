using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AyudaYa.Negocio.Servicios;

/// <summary>
/// Datos mínimos del perfil que la capa Web puede mostrar.
/// No expone la entidad de persistencia ni datos privados.
/// </summary>
public sealed record PerfilUsuarioDto(
    string NombreCompleto,
    string Email,
    IReadOnlyList<string> Roles,
    bool Activo,
    string? Dni,
    bool EsVoluntario,
    bool EmailConfirmado);

/// <summary>
/// Lo que la navegación necesita saber de quien está logueado: si es
/// voluntario, si pertenece a organizaciones y si administra. Se calcula por
/// pedido y no se guarda en la cookie, así los cambios se ven al instante.
/// </summary>
public sealed record ContextoUsuarioDto(
    Guid UsuarioId,
    string Nombre,
    bool EsAdministrador,
    bool EsVoluntario,
    bool VoluntarioActivo,
    IReadOnlyList<MembresiaDto> Organizaciones);

/// <summary>
/// Punto único para crear, autenticar, desactivar y reactivar usuarios.
///
/// La base exige Email en minúsculas y UserName = Email. UserManager
/// normaliza NormalizedEmail/NormalizedUserName, pero no baja a minúsculas
/// las columnas Email/UserName, así que esa regla se centraliza acá.
/// </summary>
public sealed class UsuarioAppService
{
    public const string RolAdministrador = "Administrador";
    public const string RolUsuario = "Usuario";

    private static readonly Regex DniValido = new("^[0-9]{6,9}$", RegexOptions.Compiled);

    /// <summary>Espera mínima entre dos correos de confirmación al mismo usuario.</summary>
    private static readonly TimeSpan EsperaReenvio = TimeSpan.FromMinutes(1);

    /// <summary>Tope de correos de recuperación por cuenta y por hora, y en total por hora.</summary>
    private const int MaximoRecuperacionesPorCuentaPorHora = 3;
    private const int MaximoRecuperacionesPorHora = 60;

    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly SignInManager<UsuarioIdentity> _signInManager;
    private readonly AyudaYaDbContext _db;
    private readonly IEnviadorCorreo _correo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UsuarioAppService> _logger;

    public UsuarioAppService(
        UserManager<UsuarioIdentity> userManager,
        SignInManager<UsuarioIdentity> signInManager,
        AyudaYaDbContext db,
        IEnviadorCorreo correo,
        IMemoryCache cache,
        ILogger<UsuarioAppService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _correo = correo;
        _cache = cache;
        _logger = logger;
    }

    public static string NormalizarEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Crea el usuario, le asigna el rol Usuario y crea su fila de datos
    /// privados (con DNI nulo si no se informó). Todo en una transacción.
    /// </summary>
    public async Task<IdentityResult> RegistrarAsync(
        string email,
        string password,
        string nombreCompleto,
        string? dni = null,
        CancellationToken cancellationToken = default)
    {
        email = NormalizarEmail(email);
        nombreCompleto = nombreCompleto.Trim();

        dni = string.IsNullOrWhiteSpace(dni) ? null : dni.Trim();
        if (dni is not null && !DniValido.IsMatch(dni))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DniInvalido",
                Description = "El DNI debe tener entre 6 y 9 dígitos."
            });
        }

        if (dni is not null &&
            await _db.UsuarioDatosPrivados.AnyAsync(x => x.Dni == dni, cancellationToken))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DniDuplicado",
                Description = "Ya existe un usuario con ese DNI."
            });
        }

        // Si el llamador ya abrió una transacción (por ejemplo, un test de
        // integración o un caso de uso más grande), no se abre otra: se
        // participa de la existente. Así el alta es atómica en ambos casos.
        var currentTransaction = _db.Database.CurrentTransaction;
        var ownsTransaction = currentTransaction is null;
        var transaction = currentTransaction
            ?? await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = new UsuarioIdentity
            {
                UserName = email,
                Email = email,
                NombreCompleto = nombreCompleto,
                Activo = true,
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                if (ownsTransaction)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return createResult;
            }

            var roleResult = await _userManager.AddToRoleAsync(user, RolUsuario);
            if (!roleResult.Succeeded)
            {
                if (ownsTransaction)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return roleResult;
            }

            _db.UsuarioDatosPrivados.Add(new UsuarioDatosPrivados
            {
                UsuarioId = user.Id,
                Dni = dni
            });

            await _db.SaveChangesAsync(cancellationToken);

            if (ownsTransaction)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return IdentityResult.Success;
        }
        catch
        {
            if (ownsTransaction)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (ownsTransaction)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Login con email normalizado. Usa el SignInManager propio, que bloquea
    /// a los usuarios con Activo = false.
    /// </summary>
    public Task<SignInResult> IniciarSesionAsync(
        string email,
        string password,
        bool recordarme)
    {
        var normalizedEmail = NormalizarEmail(email);
        return _signInManager.PasswordSignInAsync(
            normalizedEmail,
            password,
            recordarme,
            lockoutOnFailure: true);
    }

    public Task CerrarSesionAsync() => _signInManager.SignOutAsync();

    public async Task<PerfilUsuarioDto?> ObtenerPerfilAsync(
        ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var dni = await _db.UsuarioDatosPrivados.AsNoTracking()
            .Where(datos => datos.UsuarioId == user.Id)
            .Select(datos => datos.Dni)
            .SingleOrDefaultAsync();
        var esVoluntario = await _db.Voluntario.AsNoTracking()
            .AnyAsync(voluntario => voluntario.UsuarioId == user.Id);
        return new PerfilUsuarioDto(
            user.NombreCompleto,
            user.Email ?? string.Empty,
            roles.ToList(),
            user.Activo,
            dni,
            esVoluntario,
            user.EmailConfirmed);
    }

    /// <summary>Actualiza únicamente los datos del usuario autenticado.</summary>
    public async Task<IdentityResult> ActualizarPerfilAsync(
        ClaimsPrincipal principal, string nombreCompleto, string? dni,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user is null || !user.Activo)
            return IdentityResult.Failed(new IdentityError { Code = "UsuarioInvalido", Description = "La cuenta no está disponible." });

        nombreCompleto = (nombreCompleto ?? "").Trim();
        if (nombreCompleto.Length is < 3 or > 150)
            return IdentityResult.Failed(new IdentityError { Code = "NombreInvalido", Description = "El nombre debe tener entre 3 y 150 caracteres." });

        dni = string.IsNullOrWhiteSpace(dni) ? null : new string(dni.Where(c => c != '.' && !char.IsWhiteSpace(c)).ToArray());
        if (dni == "") dni = null;
        if (dni is not null && !DniValido.IsMatch(dni))
            return IdentityResult.Failed(new IdentityError { Code = "DniInvalido", Description = "El DNI debe tener entre 6 y 9 dígitos." });
        if (dni is not null && await _db.UsuarioDatosPrivados.AnyAsync(datos => datos.UsuarioId != user.Id && datos.Dni == dni, cancellationToken))
            return IdentityResult.Failed(new IdentityError { Code = "DniDuplicado", Description = "Ya existe un usuario con ese DNI." });

        var datosPrivados = await _db.UsuarioDatosPrivados.SingleOrDefaultAsync(datos => datos.UsuarioId == user.Id, cancellationToken);
        if (datosPrivados is null)
        {
            datosPrivados = new UsuarioDatosPrivados { UsuarioId = user.Id };
            _db.UsuarioDatosPrivados.Add(datosPrivados);
        }
        datosPrivados.Dni = dni;
        user.NombreCompleto = nombreCompleto;
        try
        {
            // Identity y los datos privados comparten contexto y SaveChanges.
            return await _userManager.UpdateAsync(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_usuario_datos_privados_dni" })
        {
            return IdentityResult.Failed(new IdentityError { Code = "DniDuplicado", Description = "Ya existe un usuario con ese DNI." });
        }
    }

    /// <summary>Cambia la clave comprobando la actual y renueva la sesión activa.</summary>
    public async Task<IdentityResult> CambiarPasswordAsync(
        ClaimsPrincipal principal, string actual, string nueva)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user is null || !user.Activo)
            return IdentityResult.Failed(new IdentityError { Code = "UsuarioInvalido", Description = "La cuenta no está disponible." });
        if (actual == nueva)
            return IdentityResult.Failed(new IdentityError { Code = "PasswordIgual", Description = "La contraseña nueva tiene que ser distinta de la actual." });

        var resultado = await _userManager.ChangePasswordAsync(user, actual, nueva);
        if (resultado.Succeeded) await _signInManager.RefreshSignInAsync(user);
        return resultado;
    }

    /// <summary>
    /// Baja lógica. No borra filas: conserva solicitudes, asignaciones e
    /// historial. Regenera SecurityStamp para invalidar cookies vigentes.
    /// </summary>
    public async Task<IdentityResult> DesactivarAsync(
        Guid usuarioId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "MotivoRequerido",
                Description = "El motivo de desactivación es obligatorio."
            });
        }

        var user = await _userManager.FindByIdAsync(usuarioId.ToString());
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UsuarioNoEncontrado",
                Description = "El usuario no existe."
            });
        }

        user.Activo = false;
        user.MotivoDesactivacion = motivo.Trim();
        user.DesactivadoAt = DateTime.UtcNow;
        user.SecurityStamp = Guid.NewGuid().ToString();

        return await _userManager.UpdateAsync(user);
    }

    /// <summary>Reactivación administrativa.</summary>
    public async Task<IdentityResult> ReactivarAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(usuarioId.ToString());
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UsuarioNoEncontrado",
                Description = "El usuario no existe."
            });
        }

        user.Activo = true;
        user.MotivoDesactivacion = null;
        user.DesactivadoAt = null;
        user.SecurityStamp = Guid.NewGuid().ToString();

        return await _userManager.UpdateAsync(user);
    }

    /// <summary>Contexto de navegación del usuario autenticado, o null si no corresponde.</summary>
    public async Task<ContextoUsuarioDto?> ObtenerContextoAsync(
        ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true
            || !Guid.TryParse(_userManager.GetUserId(principal), out var usuarioId))
        {
            return null;
        }

        var voluntario = await _db.Voluntario.AsNoTracking()
            .Where(v => v.UsuarioId == usuarioId)
            .Select(v => new { v.Activo })
            .SingleOrDefaultAsync(cancellationToken);

        var organizaciones = await _db.OrganizacionUsuario.AsNoTracking()
            .Where(ou => ou.UsuarioId == usuarioId && ou.Activo)
            .OrderByDescending(ou => ou.Rol == "responsable")
            .ThenBy(ou => ou.Organizacion.Nombre)
            .Select(ou => new MembresiaDto(
                ou.OrganizacionId, ou.Organizacion.Nombre, ou.Organizacion.Estado, ou.Rol))
            .ToListAsync(cancellationToken);

        return new ContextoUsuarioDto(
            usuarioId,
            principal.Identity?.Name ?? string.Empty,
            principal.IsInRole(RolAdministrador),
            voluntario is not null,
            voluntario?.Activo ?? false,
            organizaciones);
    }

    /// <summary>
    /// Genera el token de confirmación de email y envía el correo con el enlace.
    /// <paramref name="construirEnlace"/> arma la URL absoluta a partir del id y
    /// del token ya codificado para URL. Devuelve el enlace (solo para que la
    /// capa Web lo muestre en desarrollo). Limita el reenvío a uno por minuto.
    /// </summary>
    public async Task<Resultado<string>> EnviarConfirmacionAsync(
        Guid usuarioId,
        Func<Guid, string, string> construirEnlace,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(usuarioId.ToString());
        if (user is null || !user.Activo)
        {
            return Resultado<string>.Falla(Codigos.UsuarioInvalido, "El usuario no existe o está inactivo.");
        }

        if (user.EmailConfirmed)
        {
            return Resultado<string>.Falla("YaConfirmado", "Tu correo ya está confirmado.");
        }

        var clave = $"confirmacion:{usuarioId}";
        if (_cache.TryGetValue(clave, out _))
        {
            return Resultado<string>.Falla(
                "EsperaReenvio", "Ya te enviamos un correo hace instantes. Esperá un minuto para pedir otro.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var tokenCodificado = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var enlace = construirEnlace(user.Id, tokenCodificado);

        try
        {
            await _correo.EnviarAsync(
                user.Email!,
                "Confirmá tu correo en AyudaYa",
                string.Join(Environment.NewLine,
                    $"Hola {user.NombreCompleto},",
                    string.Empty,
                    "Para confirmar tu correo y poder pedir ayuda o sumarte como voluntario, abrí este enlace:",
                    enlace,
                    string.Empty,
                    "Si no creaste una cuenta en AyudaYa, ignorá este mensaje."),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Un fallo del proveedor de correo no debe tumbar el alta ni mostrar un error técnico.
            _logger.LogError(ex, "No se pudo enviar el correo de confirmación al usuario {UsuarioId}.", usuarioId);
            return Resultado<string>.Falla(
                "CorreoNoEnviado",
                "No pudimos enviar el correo en este momento. Probá de nuevo en unos minutos desde tu perfil.");
        }

        _cache.Set(clave, true, EsperaReenvio);
        return Resultado<string>.Ok(enlace);
    }

    /// <summary>
    /// Pide recuperar la contraseña. Si el correo pertenece a una cuenta activa,
    /// le envía un enlace de un solo uso que vence en una hora. No informa si
    /// el correo existe (siempre termina igual para quien lo pide) y limita a
    /// un correo por minuto por cuenta. <paramref name="construirEnlace"/> arma
    /// la URL absoluta con el id y el token ya codificado para URL.
    /// </summary>
    public async Task SolicitarRestablecimientoAsync(
        string? email,
        Func<Guid, string, string> construirEnlace,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
        {
            return;
        }

        var user = await _userManager.FindByEmailAsync(NormalizarEmail(email));
        if (user is null || !user.Activo)
        {
            return;
        }

        var clave = $"restablecer:{user.Id}";
        if (_cache.TryGetValue(clave, out _))
        {
            return;
        }

        // Topes para que nadie use la función para llenar un buzón ni agotar el cupo diario de correos.
        if (!ReservarCupo($"restablecer-hora:{user.Id}", MaximoRecuperacionesPorCuentaPorHora)
            || !ReservarCupo("restablecer-hora:global", MaximoRecuperacionesPorHora))
        {
            return;
        }

        // Se reserva el cupo antes de enviar: aunque el envío falle, no se puede insistir en bucle.
        _cache.Set(clave, true, EsperaReenvio);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var enlace = construirEnlace(user.Id, WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)));

        try
        {
            await _correo.EnviarAsync(
                user.Email!,
                "Recuperá tu contraseña de AyudaYa",
                string.Join(Environment.NewLine,
                    $"Hola {user.NombreCompleto},",
                    string.Empty,
                    "Pediste cambiar tu contraseña. Para elegir una nueva, abrí este enlace (vale por 1 hora y se puede usar una sola vez):",
                    enlace,
                    string.Empty,
                    "Si no lo pediste vos, ignorá este mensaje: tu contraseña actual sigue siendo la misma."),
                cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "No se pudo enviar el correo de recuperación al usuario {UsuarioId}.", user.Id);
        }
    }

    private sealed record CupoHora(int Usados, DateTimeOffset Vence);

    /// <summary>Cuenta un uso en una ventana fija de una hora. Devuelve false si ya se alcanzó el tope.</summary>
    private bool ReservarCupo(string clave, int maximo)
    {
        lock (_cache)
        {
            var ahora = DateTimeOffset.UtcNow;
            var actual = _cache.TryGetValue(clave, out CupoHora? guardado) && guardado!.Vence > ahora
                ? guardado
                : new CupoHora(0, ahora.AddHours(1));

            if (actual.Usados >= maximo)
            {
                return false;
            }

            _cache.Set(clave, actual with { Usados = actual.Usados + 1 }, actual.Vence);
            return true;
        }
    }

    /// <summary>
    /// Fija la contraseña nueva con el enlace recibido por correo. El token es de
    /// un solo uso: al cambiar la contraseña se renueva el SecurityStamp, lo que
    /// también cierra las sesiones abiertas. Quien demuestra ser dueño del correo
    /// recupera además el acceso si la cuenta estaba bloqueada por intentos.
    /// </summary>
    public async Task<IdentityResult> RestablecerClaveAsync(
        Guid usuarioId, string tokenCodificado, string nuevaClave, CancellationToken cancellationToken = default)
    {
        var enlaceInvalido = IdentityResult.Failed(new IdentityError
        {
            Code = "TokenInvalido",
            Description = "El enlace no es válido o venció. Pedí uno nuevo."
        });

        var user = await _userManager.FindByIdAsync(usuarioId.ToString());
        if (user is null || !user.Activo)
        {
            return enlaceInvalido;
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(tokenCodificado));
        }
        catch (FormatException)
        {
            return enlaceInvalido;
        }

        var resultado = await _userManager.ResetPasswordAsync(user, token, nuevaClave);
        if (!resultado.Succeeded)
        {
            // Los errores de la política de contraseña se muestran; cualquier otro es un enlace malo.
            return resultado.Errors.Any(e => e.Code.StartsWith("Password", StringComparison.Ordinal))
                ? resultado
                : enlaceInvalido;
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        try
        {
            await _correo.EnviarAsync(
                user.Email!,
                "Cambiaste tu contraseña de AyudaYa",
                string.Join(Environment.NewLine,
                    $"Hola {user.NombreCompleto},",
                    string.Empty,
                    "Te avisamos que la contraseña de tu cuenta de AyudaYa se cambió hace instantes y se cerraron las sesiones abiertas.",
                    "Si no fuiste vos, pedí otra recuperación de contraseña desde la pantalla de inicio de sesión."),
                cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudo enviar el aviso de cambio de contraseña al usuario {UsuarioId}.", user.Id);
        }

        return resultado;
    }

    public async Task<IdentityResult> ConfirmarEmailAsync(Guid usuarioId, string tokenCodificado)
    {
        var user = await _userManager.FindByIdAsync(usuarioId.ToString());
        if (user is null || !user.Activo)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UsuarioNoEncontrado",
                Description = "El enlace no es válido."
            });
        }

        if (user.EmailConfirmed)
        {
            return IdentityResult.Success;
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(tokenCodificado));
        }
        catch (FormatException)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "TokenInvalido",
                Description = "El enlace no es válido o venció."
            });
        }

        var resultado = await _userManager.ConfirmEmailAsync(user, token);
        return resultado.Succeeded
            ? resultado
            : IdentityResult.Failed(new IdentityError
            {
                Code = "TokenInvalido",
                Description = "El enlace no es válido o venció. Pedí uno nuevo desde tu perfil."
            });
    }

    public Task<UsuarioIdentity?> ObtenerPorEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return _userManager.FindByEmailAsync(NormalizarEmail(email));
    }

    public async Task<IdentityResult> AsignarRolAsync(
        UsuarioIdentity user,
        string rol,
        CancellationToken cancellationToken = default)
    {
        return await _userManager.AddToRoleAsync(user, rol);
    }
}

using System.Security.Claims;
using System.Text.RegularExpressions;
using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    bool EsVoluntario);

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

    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly SignInManager<UsuarioIdentity> _signInManager;
    private readonly AyudaYaDbContext _db;

    public UsuarioAppService(
        UserManager<UsuarioIdentity> userManager,
        SignInManager<UsuarioIdentity> signInManager,
        AyudaYaDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
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
            .AnyAsync(voluntario => voluntario.UsuarioId == user.Id && voluntario.Activo);
        return new PerfilUsuarioDto(
            user.NombreCompleto,
            user.Email ?? string.Empty,
            roles.ToList(),
            user.Activo,
            dni,
            esVoluntario);
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

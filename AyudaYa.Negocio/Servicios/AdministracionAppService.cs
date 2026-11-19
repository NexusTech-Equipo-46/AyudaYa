using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Negocio.Servicios;

public sealed record PanelAdminDto(
    int OrganizacionesPendientes,
    int CentrosPendientes,
    int SolicitudesAbiertas,
    int SolicitudesCriticas,
    int UsuariosActivos,
    int VoluntariosActivos,
    int CiudadesActivas);

public sealed record CiudadAdminDto(
    Guid Id,
    string Nombre,
    string Provincia,
    bool Activa,
    IReadOnlyList<ZonaDto> Zonas);

public sealed record OrganizacionAdminDto(
    Guid Id,
    string Nombre,
    string Tipo,
    string Estado,
    string? MotivoEstado,
    string CiudadNombre,
    string EmailContacto,
    string TelefonoContacto,
    string? Descripcion,
    string RegistradaPorNombre,
    string RegistradaPorEmail,
    DateTime CreadoAt,
    int Centros);

public sealed record CentroAdminDto(
    Guid Id,
    string Nombre,
    string OrganizacionNombre,
    string OrganizacionEstado,
    string TipoNombre,
    string ZonaNombre,
    string CiudadNombre,
    string Direccion,
    string Telefono,
    string Estado,
    string? MotivoEstado,
    string? Descripcion,
    double? Latitud,
    double? Longitud,
    DateTime CreadoAt);

public sealed record UsuarioAdminDto(
    Guid Id,
    string Nombre,
    string Email,
    bool EmailConfirmado,
    bool Activo,
    string? MotivoDesactivacion,
    bool EsAdministrador,
    bool EsVoluntario,
    string? Dni,
    DateTime CreadoAt);

public sealed record EventoAdminDto(
    Guid Id,
    DateTime Cuando,
    string Administrador,
    string Entidad,
    Guid? EntidadId,
    string Accion,
    string? Detalle);

/// <summary>
/// Acciones de administración: geografía, aprobaciones, usuarios y auditoría.
/// Cada método exige que quien actúa sea un administrador activo y cada
/// cambio queda registrado en evento_administrativo.
/// </summary>
public sealed class AdministracionAppService : ServicioBase
{
    public const int LargoMaximoMotivo = 300;
    public const int TamanioPaginaAuditoria = 50;

    private readonly UsuarioAppService _usuarios;
    private readonly SolicitudAppService _solicitudes;

    public AdministracionAppService(
        AyudaYaDbContext db,
        UserManager<UsuarioIdentity> userManager,
        UsuarioAppService usuarios,
        SolicitudAppService solicitudes)
        : base(db, userManager)
    {
        _usuarios = usuarios;
        _solicitudes = solicitudes;
    }

    private async Task<ErrorNegocio?> VerificarAdminAsync(Guid adminId)
    {
        var actor = await ObtenerActorActivoAsync(adminId);
        if (actor is null)
        {
            return UsuarioInvalido();
        }

        return await EsAdministradorAsync(actor)
            ? null
            : NoAutorizado("Esta acción es solo para administradores.");
    }

    private void RegistrarEvento(
        Guid adminId, string entidad, Guid? entidadId, string accion, string? detalle)
    {
        Db.EventoAdministrativo.Add(new EventoAdministrativo
        {
            Id = Guid.NewGuid(),
            AdministradorId = adminId,
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            Detalle = detalle,
            CreadoAt = DateTime.UtcNow
        });
    }

    // ------------------------------------------------------------------
    // Panel
    // ------------------------------------------------------------------

    public async Task<Resultado<PanelAdminDto>> ObtenerPanelAsync(
        Guid adminId, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return Resultado<PanelAdminDto>.Falla([error]);
        }

        var orgs = await Db.Organizacion.AsNoTracking()
            .CountAsync(o => o.Estado == "pendiente", cancellationToken);
        var centros = await Db.Centro.AsNoTracking()
            .CountAsync(c => c.EstadoVerificacion == "pendiente", cancellationToken);
        var abiertas = await Db.Solicitud.AsNoTracking().CountAsync(
            s => s.Estado == "pendiente" || s.Estado == "asignada"
                || s.Estado == "en_atencion" || s.Estado == "parcial",
            cancellationToken);
        var criticas = await Db.Solicitud.AsNoTracking().CountAsync(
            s => s.Urgencia == "critica"
                && (s.Estado == "pendiente" || s.Estado == "asignada"
                    || s.Estado == "en_atencion" || s.Estado == "parcial"),
            cancellationToken);
        var usuarios = await Db.Users.AsNoTracking().CountAsync(u => u.Activo, cancellationToken);
        var voluntarios = await Db.Voluntario.AsNoTracking().CountAsync(v => v.Activo, cancellationToken);
        var ciudades = await Db.Ciudad.AsNoTracking().CountAsync(c => c.Activa, cancellationToken);

        return Resultado<PanelAdminDto>.Ok(new PanelAdminDto(
            orgs, centros, abiertas, criticas, usuarios, voluntarios, ciudades));
    }

    // ------------------------------------------------------------------
    // Ciudades y zonas
    // ------------------------------------------------------------------

    public async Task<Resultado<List<CiudadAdminDto>>> ListarCiudadesAsync(
        Guid adminId, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return Resultado<List<CiudadAdminDto>>.Falla([error]);
        }

        var filas = await Db.Ciudad.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                c.Provincia,
                c.Activa,
                Zonas = c.Zona
                    .OrderBy(z => z.Nombre)
                    .Select(z => new ZonaDto(
                        z.Id, z.CiudadId, z.Nombre, z.Activa, z.LatitudCentro, z.LongitudCentro))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Resultado<List<CiudadAdminDto>>.Ok(filas
            .Select(c => new CiudadAdminDto(c.Id, c.Nombre, c.Provincia, c.Activa, c.Zonas))
            .ToList());
    }

    public async Task<Resultado<Guid>> CrearCiudadAsync(
        Guid adminId, string nombre, string provincia, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return Resultado<Guid>.Falla([error]);
        }

        var nombreLimpio = Limpiar(nombre);
        var provinciaLimpia = Limpiar(provincia);
        if (nombreLimpio is null || provinciaLimpia is null
            || nombreLimpio.Length > 100 || provinciaLimpia.Length > 100)
        {
            return Resultado<Guid>.Falla(
                "DatosInvalidos", "Ingresá el nombre y la provincia (hasta 100 caracteres cada uno).");
        }

        var nombreMin = nombreLimpio.ToLower();
        var provMin = provinciaLimpia.ToLower();
        if (await Db.Ciudad.AsNoTracking().AnyAsync(
            c => c.Nombre.ToLower() == nombreMin && c.Provincia.ToLower() == provMin
                && c.Pais.ToLower() == "argentina",
            cancellationToken))
        {
            return Resultado<Guid>.Falla("CiudadDuplicada", "Esa ciudad ya existe.");
        }

        var ahora = DateTime.UtcNow;
        var ciudad = new Ciudad
        {
            Id = Guid.NewGuid(),
            Nombre = nombreLimpio,
            Provincia = provinciaLimpia,
            Pais = "Argentina",
            Activa = true,
            CreadoAt = ahora,
            ActualizadoAt = ahora
        };

        Db.Ciudad.Add(ciudad);
        RegistrarEvento(adminId, "ciudad", ciudad.Id, "crear", $"{nombreLimpio}, {provinciaLimpia}");
        await Db.SaveChangesAsync(cancellationToken);
        return Resultado<Guid>.Ok(ciudad.Id);
    }

    public async Task<ResultadoOperacion> CambiarEstadoCiudadAsync(
        Guid adminId, Guid ciudadId, bool activa, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return ResultadoOperacion.Falla(error);
        }

        var ciudad = await Db.Ciudad.SingleOrDefaultAsync(c => c.Id == ciudadId, cancellationToken);
        if (ciudad is null)
        {
            return ResultadoOperacion.Falla(Error("CiudadNoEncontrada", "La ciudad no existe."));
        }

        ciudad.Activa = activa;
        RegistrarEvento(adminId, "ciudad", ciudad.Id, activa ? "activar" : "desactivar", ciudad.Nombre);
        await Db.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion.Ok();
    }

    public async Task<Resultado<Guid>> CrearZonaAsync(
        Guid adminId,
        Guid ciudadId,
        string nombre,
        double? latitud,
        double? longitud,
        CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return Resultado<Guid>.Falla([error]);
        }

        var validacion = ValidarZona(nombre, latitud, longitud, out var nombreLimpio);
        if (validacion is not null)
        {
            return Resultado<Guid>.Falla([validacion]);
        }

        if (!await Db.Ciudad.AsNoTracking().AnyAsync(c => c.Id == ciudadId, cancellationToken))
        {
            return Resultado<Guid>.Falla("CiudadNoEncontrada", "La ciudad no existe.");
        }

        var nombreMin = nombreLimpio!.ToLower();
        if (await Db.Zona.AsNoTracking().AnyAsync(
            z => z.CiudadId == ciudadId && z.Nombre.ToLower() == nombreMin, cancellationToken))
        {
            return Resultado<Guid>.Falla("ZonaDuplicada", "Ya existe una zona con ese nombre en la ciudad.");
        }

        var ahora = DateTime.UtcNow;
        var zona = new Zona
        {
            Id = Guid.NewGuid(),
            CiudadId = ciudadId,
            Nombre = nombreLimpio,
            Activa = true,
            LatitudCentro = latitud,
            LongitudCentro = longitud,
            CreadoAt = ahora,
            ActualizadoAt = ahora
        };

        Db.Zona.Add(zona);
        RegistrarEvento(adminId, "zona", zona.Id, "crear", nombreLimpio);
        await Db.SaveChangesAsync(cancellationToken);
        return Resultado<Guid>.Ok(zona.Id);
    }

    public async Task<ResultadoOperacion> ActualizarZonaAsync(
        Guid adminId,
        Guid zonaId,
        string nombre,
        double? latitud,
        double? longitud,
        bool activa,
        CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return ResultadoOperacion.Falla(error);
        }

        var validacion = ValidarZona(nombre, latitud, longitud, out var nombreLimpio);
        if (validacion is not null)
        {
            return ResultadoOperacion.Falla(validacion);
        }

        var zona = await Db.Zona.SingleOrDefaultAsync(z => z.Id == zonaId, cancellationToken);
        if (zona is null)
        {
            return ResultadoOperacion.Falla(Error("ZonaNoEncontrada", "La zona no existe."));
        }

        var nombreMin = nombreLimpio!.ToLower();
        if (await Db.Zona.AsNoTracking().AnyAsync(
            z => z.CiudadId == zona.CiudadId && z.Id != zonaId && z.Nombre.ToLower() == nombreMin,
            cancellationToken))
        {
            return ResultadoOperacion.Falla(Error(
                "ZonaDuplicada", "Ya existe una zona con ese nombre en la ciudad."));
        }

        zona.Nombre = nombreLimpio;
        zona.LatitudCentro = latitud;
        zona.LongitudCentro = longitud;
        zona.Activa = activa;
        RegistrarEvento(adminId, "zona", zona.Id, "editar", nombreLimpio);
        await Db.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion.Ok();
    }

    private static ErrorNegocio? ValidarZona(
        string nombre, double? latitud, double? longitud, out string? nombreLimpio)
    {
        nombreLimpio = Limpiar(nombre);
        if (nombreLimpio is null || nombreLimpio.Length > 100)
        {
            return new ErrorNegocio("NombreInvalido", "Ingresá el nombre de la zona (hasta 100 caracteres).");
        }

        if ((latitud is null) != (longitud is null)
            || (latitud is not null && !ReglasNegocio.CoordenadasValidas(latitud, longitud)))
        {
            return new ErrorNegocio(
                "CoordenadasInvalidas",
                "La latitud y la longitud tienen que cargarse juntas y estar en rango.");
        }

        return null;
    }

    // ------------------------------------------------------------------
    // Organizaciones y centros
    // ------------------------------------------------------------------

    public async Task<Resultado<List<OrganizacionAdminDto>>> ListarOrganizacionesAsync(
        Guid adminId, string? estado, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return Resultado<List<OrganizacionAdminDto>>.Falla([error]);
        }

        var consulta = Db.Organizacion.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            consulta = consulta.Where(o => o.Estado == estado);
        }

        var lista = await consulta
            .OrderBy(o => o.Estado == "pendiente" ? 0 : 1)
            .ThenByDescending(o => o.CreadoAt)
            .Select(o => new OrganizacionAdminDto(
                o.Id,
                o.Nombre,
                o.Tipo,
                o.Estado,
                o.MotivoEstado,
                o.Ciudad.Nombre,
                o.EmailContacto,
                o.TelefonoContacto,
                o.Descripcion,
                o.RegistradaPorNavigation.NombreCompleto,
                o.RegistradaPorNavigation.Email ?? string.Empty,
                o.CreadoAt,
                o.Centro.Count))
            .ToListAsync(cancellationToken);

        return Resultado<List<OrganizacionAdminDto>>.Ok(lista);
    }

    /// <summary>
    /// Aprueba, rechaza, suspende o reactiva una organización. Rechazar y
    /// suspender exigen motivo.
    /// </summary>
    public async Task<ResultadoOperacion> CambiarEstadoOrganizacionAsync(
        Guid adminId,
        Guid organizacionId,
        string nuevoEstado,
        string? motivo,
        CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return ResultadoOperacion.Falla(error);
        }

        var organizacion = await Db.Organizacion
            .SingleOrDefaultAsync(o => o.Id == organizacionId, cancellationToken);
        if (organizacion is null)
        {
            return ResultadoOperacion.Falla(Error("OrganizacionNoEncontrada", "La organización no existe."));
        }

        var motivoLimpio = Limpiar(motivo);
        var transicionValida = (nuevoEstado, organizacion.Estado) switch
        {
            ("activa", "pendiente" or "rechazada" or "suspendida") => true,
            ("rechazada", "pendiente") => true,
            ("suspendida", "activa") => true,
            _ => false
        };

        if (!transicionValida)
        {
            return ResultadoOperacion.Falla(Error(
                "TransicionInvalida", "Ese cambio de estado no corresponde."));
        }

        if (nuevoEstado is "rechazada" or "suspendida")
        {
            if (motivoLimpio is null)
            {
                return ResultadoOperacion.Falla(Error("MotivoRequerido", "Indicá el motivo."));
            }

            if (motivoLimpio.Length > LargoMaximoMotivo)
            {
                return ResultadoOperacion.Falla(Error(
                    "MotivoMuyLargo", $"El motivo no puede superar los {LargoMaximoMotivo} caracteres."));
            }
        }

        organizacion.Estado = nuevoEstado;
        organizacion.MotivoEstado = nuevoEstado == "activa" ? null : motivoLimpio;
        if (nuevoEstado == "activa")
        {
            organizacion.AprobadaPor = adminId;
            organizacion.AprobadaAt = DateTime.UtcNow;
        }

        RegistrarEvento(
            adminId, "organizacion", organizacion.Id, nuevoEstado,
            motivoLimpio is null ? organizacion.Nombre : $"{organizacion.Nombre}: {motivoLimpio}");

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Falla(Error(
                Codigos.ConflictoDeEstado, "No se pudo guardar el cambio. Volvé a intentar."));
        }

        return ResultadoOperacion.Ok();
    }

    public async Task<Resultado<List<CentroAdminDto>>> ListarCentrosAsync(
        Guid adminId, string? estado, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return Resultado<List<CentroAdminDto>>.Falla([error]);
        }

        var consulta = Db.Centro.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            consulta = consulta.Where(c => c.EstadoVerificacion == estado);
        }

        var lista = await consulta
            .OrderBy(c => c.EstadoVerificacion == "pendiente" ? 0 : 1)
            .ThenByDescending(c => c.CreadoAt)
            .Select(c => new CentroAdminDto(
                c.Id,
                c.Nombre,
                c.Organizacion.Nombre,
                c.Organizacion.Estado,
                c.TipoCentro.Nombre,
                c.Zona.Nombre,
                c.Zona.Ciudad.Nombre,
                c.Direccion,
                c.TelefonoContacto,
                c.EstadoVerificacion,
                c.MotivoEstado,
                c.Descripcion,
                c.Latitud,
                c.Longitud,
                c.CreadoAt))
            .ToListAsync(cancellationToken);

        return Resultado<List<CentroAdminDto>>.Ok(lista);
    }

    public async Task<ResultadoOperacion> CambiarEstadoCentroAsync(
        Guid adminId,
        Guid centroId,
        string nuevoEstado,
        string? motivo,
        CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return ResultadoOperacion.Falla(error);
        }

        var centro = await Db.Centro.SingleOrDefaultAsync(c => c.Id == centroId, cancellationToken);
        if (centro is null)
        {
            return ResultadoOperacion.Falla(Error("CentroNoEncontrado", "El centro no existe."));
        }

        var motivoLimpio = Limpiar(motivo);
        var transicionValida = (nuevoEstado, centro.EstadoVerificacion) switch
        {
            ("aprobado", "pendiente" or "rechazado" or "suspendido") => true,
            ("rechazado", "pendiente") => true,
            ("suspendido", "aprobado") => true,
            _ => false
        };

        if (!transicionValida)
        {
            return ResultadoOperacion.Falla(Error(
                "TransicionInvalida", "Ese cambio de estado no corresponde."));
        }

        if (nuevoEstado is "rechazado" or "suspendido")
        {
            if (motivoLimpio is null)
            {
                return ResultadoOperacion.Falla(Error("MotivoRequerido", "Indicá el motivo."));
            }

            if (motivoLimpio.Length > LargoMaximoMotivo)
            {
                return ResultadoOperacion.Falla(Error(
                    "MotivoMuyLargo", $"El motivo no puede superar los {LargoMaximoMotivo} caracteres."));
            }
        }

        // Reactivar un rechazado podría duplicar un lugar que otra organización
        // cargó mientras tanto: se aplica la misma regla que al crearlo.
        if (nuevoEstado == "aprobado"
            && await ExisteOtroCentroEnElLugarAsync(
                centro.CiudadId, centro.TipoCentroId, centro.Direccion, centro.Id, cancellationToken))
        {
            return ResultadoOperacion.Falla(CentroYaRegistrado(
                "Ya hay otro centro de ese tipo registrado en esa dirección. Revisalo antes de aprobar este."));
        }

        centro.EstadoVerificacion = nuevoEstado;
        centro.MotivoEstado = nuevoEstado == "aprobado" ? null : motivoLimpio;
        if (nuevoEstado == "aprobado")
        {
            centro.AprobadoPor = adminId;
            centro.AprobadoAt = DateTime.UtcNow;
        }

        RegistrarEvento(
            adminId, "centro", centro.Id, nuevoEstado,
            motivoLimpio is null ? centro.Nombre : $"{centro.Nombre}: {motivoLimpio}");

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Falla(Error(
                Codigos.ConflictoDeEstado, "No se pudo guardar el cambio. Volvé a intentar."));
        }

        return ResultadoOperacion.Ok();
    }

    // ------------------------------------------------------------------
    // Usuarios
    // ------------------------------------------------------------------

    /// <summary>Busca por nombre o correo. Solo un administrador ve el DNI.</summary>
    public async Task<Resultado<List<UsuarioAdminDto>>> ListarUsuariosAsync(
        Guid adminId, string? busqueda, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return Resultado<List<UsuarioAdminDto>>.Falla([error]);
        }

        var consulta = Db.Users.AsNoTracking().AsQueryable();
        var texto = Limpiar(busqueda)?.ToLower();
        if (texto is not null)
        {
            consulta = consulta.Where(u =>
                u.NombreCompleto.ToLower().Contains(texto)
                || (u.Email != null && u.Email.Contains(texto)));
        }

        var rolAdminId = await Db.Roles.AsNoTracking()
            .Where(r => r.NormalizedName == UsuarioAppService.RolAdministrador.ToUpperInvariant())
            .Select(r => (Guid?)r.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var filas = await consulta
            .OrderByDescending(u => u.CreadoAt)
            .Take(200)
            .Select(u => new
            {
                u.Id,
                u.NombreCompleto,
                u.Email,
                u.EmailConfirmed,
                u.Activo,
                u.MotivoDesactivacion,
                u.CreadoAt,
                Dni = u.UsuarioDatosPrivados == null ? null : u.UsuarioDatosPrivados.Dni,
                EsVoluntario = u.Voluntario != null
            })
            .ToListAsync(cancellationToken);

        var idsAdmin = rolAdminId is null
            ? new HashSet<Guid>()
            : (await Db.UserRoles.AsNoTracking()
                .Where(ur => ur.RoleId == rolAdminId)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken)).ToHashSet();

        return Resultado<List<UsuarioAdminDto>>.Ok(filas.Select(f => new UsuarioAdminDto(
            f.Id,
            f.NombreCompleto,
            f.Email ?? string.Empty,
            f.EmailConfirmed,
            f.Activo,
            f.MotivoDesactivacion,
            idsAdmin.Contains(f.Id),
            f.EsVoluntario,
            f.Dni,
            f.CreadoAt)).ToList());
    }

    public async Task<ResultadoOperacion> DesactivarUsuarioAsync(
        Guid adminId, Guid usuarioId, string motivo, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return ResultadoOperacion.Falla(error);
        }

        var motivoLimpio = Limpiar(motivo);
        if (motivoLimpio is null)
        {
            return ResultadoOperacion.Falla(Error("MotivoRequerido", "Indicá el motivo de la desactivación."));
        }

        if (motivoLimpio.Length > LargoMaximoMotivo)
        {
            return ResultadoOperacion.Falla(Error(
                "MotivoMuyLargo", $"El motivo no puede superar los {LargoMaximoMotivo} caracteres."));
        }

        if (usuarioId == adminId)
        {
            return ResultadoOperacion.Falla(Error(
                "AutoDesactivacion", "No podés desactivar tu propia cuenta."));
        }

        var usuario = await UserManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return ResultadoOperacion.Falla(Error("UsuarioNoEncontrado", "El usuario no existe."));
        }

        if (!usuario.Activo)
        {
            return ResultadoOperacion.Falla(Error("YaInactivo", "El usuario ya está desactivado."));
        }

        if (await UserManager.IsInRoleAsync(usuario, UsuarioAppService.RolAdministrador)
            && await ContarAdministradoresActivosAsync(cancellationToken) <= 1)
        {
            return ResultadoOperacion.Falla(Error(
                "UltimoAdministrador", "No se puede desactivar al último administrador activo."));
        }

        var esUltimoResponsable = await Db.OrganizacionUsuario.AsNoTracking().AnyAsync(
            ou => ou.UsuarioId == usuarioId
                && ou.Rol == "responsable"
                && ou.Activo
                && ou.Organizacion.Tipo == "comunitaria"
                && !Db.OrganizacionUsuario.Any(otro =>
                    otro.OrganizacionId == ou.OrganizacionId
                    && otro.Id != ou.Id
                    && otro.Rol == "responsable"
                    && otro.Activo
                    && otro.Usuario.Activo),
            cancellationToken);
        if (esUltimoResponsable)
        {
            return ResultadoOperacion.Falla(Error(
                "UltimoResponsable",
                "Es la única persona responsable de una organización comunitaria. Primero hay que sumar a otro responsable."));
        }

        IdentityResult resultado;
        try
        {
            resultado = await _usuarios.DesactivarAsync(usuarioId, motivoLimpio, cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Falla(Error(
                Codigos.ConflictoDeEstado, "No se pudo desactivar al usuario. Volvé a intentar."));
        }

        if (!resultado.Succeeded)
        {
            return ResultadoOperacion.Falla(Error(
                "UsuarioNoActualizado", string.Join(" ", resultado.Errors.Select(e => e.Description))));
        }

        RegistrarEvento(adminId, "usuario", usuarioId, "desactivar", motivoLimpio);
        await Db.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> ReactivarUsuarioAsync(
        Guid adminId, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return ResultadoOperacion.Falla(error);
        }

        var resultado = await _usuarios.ReactivarAsync(usuarioId, cancellationToken);
        if (!resultado.Succeeded)
        {
            return ResultadoOperacion.Falla(Error(
                "UsuarioNoActualizado", string.Join(" ", resultado.Errors.Select(e => e.Description))));
        }

        RegistrarEvento(adminId, "usuario", usuarioId, "reactivar", null);
        await Db.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion.Ok();
    }

    /// <summary>Da o quita el rol de administrador. Siempre tiene que quedar al menos uno activo.</summary>
    public async Task<ResultadoOperacion> CambiarRolAdministradorAsync(
        Guid adminId, Guid usuarioId, bool esAdministrador, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return ResultadoOperacion.Falla(error);
        }

        var usuario = await UserManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null || !usuario.Activo)
        {
            return ResultadoOperacion.Falla(Error(
                "UsuarioNoEncontrado", "El usuario no existe o está desactivado."));
        }

        var yaEsAdmin = await UserManager.IsInRoleAsync(usuario, UsuarioAppService.RolAdministrador);
        if (yaEsAdmin == esAdministrador)
        {
            return ResultadoOperacion.Ok();
        }

        if (!esAdministrador && await ContarAdministradoresActivosAsync(cancellationToken) <= 1)
        {
            return ResultadoOperacion.Falla(Error(
                "UltimoAdministrador", "Tiene que quedar al menos un administrador activo."));
        }

        var resultado = esAdministrador
            ? await UserManager.AddToRoleAsync(usuario, UsuarioAppService.RolAdministrador)
            : await UserManager.RemoveFromRoleAsync(usuario, UsuarioAppService.RolAdministrador);

        if (!resultado.Succeeded)
        {
            return ResultadoOperacion.Falla(Error(
                "UsuarioNoActualizado", string.Join(" ", resultado.Errors.Select(e => e.Description))));
        }

        // Cambia el sello de seguridad para que el rol nuevo rija en la próxima revalidación.
        await UserManager.UpdateSecurityStampAsync(usuario);

        RegistrarEvento(
            adminId, "usuario", usuarioId, esAdministrador ? "dar_rol_administrador" : "quitar_rol_administrador", null);
        await Db.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion.Ok();
    }

    private async Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken)
    {
        var activos = await UserManager.GetUsersInRoleAsync(UsuarioAppService.RolAdministrador);
        return activos.Count(u => u.Activo);
    }

    // ------------------------------------------------------------------
    // Solicitudes y auditoría
    // ------------------------------------------------------------------

    /// <summary>Cancela una solicitud como administrador y deja constancia en la auditoría.</summary>
    public async Task<ResultadoOperacion> CancelarSolicitudAsync(
        Guid adminId, Guid solicitudId, string motivo, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return ResultadoOperacion.Falla(error);
        }

        var resultado = await _solicitudes.CancelarAsync(adminId, solicitudId, motivo, cancellationToken);
        if (!resultado.Exito)
        {
            return resultado;
        }

        RegistrarEvento(adminId, "solicitud", solicitudId, "cancelar", Limpiar(motivo));
        await Db.SaveChangesAsync(cancellationToken);
        return resultado;
    }

    public async Task<Resultado<List<EventoAdminDto>>> ListarEventosAsync(
        Guid adminId, string? entidad, int pagina, CancellationToken cancellationToken = default)
    {
        var error = await VerificarAdminAsync(adminId);
        if (error is not null)
        {
            return Resultado<List<EventoAdminDto>>.Falla([error]);
        }

        var consulta = Db.EventoAdministrativo.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entidad))
        {
            consulta = consulta.Where(e => e.Entidad == entidad);
        }

        var lista = await consulta
            .OrderByDescending(e => e.CreadoAt)
            .Skip(Math.Max(pagina - 1, 0) * TamanioPaginaAuditoria)
            .Take(TamanioPaginaAuditoria)
            .Select(e => new EventoAdminDto(
                e.Id,
                e.CreadoAt,
                e.Administrador.NombreCompleto,
                e.Entidad,
                e.EntidadId,
                e.Accion,
                e.Detalle))
            .ToListAsync(cancellationToken);

        return Resultado<List<EventoAdminDto>>.Ok(lista);
    }
}

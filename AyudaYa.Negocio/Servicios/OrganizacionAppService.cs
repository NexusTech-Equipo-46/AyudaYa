using System.Net.Mail;
using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Negocio.Servicios;

public sealed record MembresiaDto(
    Guid OrganizacionId,
    string OrganizacionNombre,
    string OrganizacionEstado,
    string Rol);

public sealed record OrganizacionDto(
    Guid Id,
    string Nombre,
    string Tipo,
    string Estado,
    string? MotivoEstado,
    Guid CiudadId,
    string CiudadNombre,
    string EmailContacto,
    string TelefonoContacto,
    string? Descripcion,
    DateTime? AprobadaAt)
{
    public bool EstaActiva => Estado == "activa";
}

public sealed record HorarioDto(
    int DiaSemana,
    string? Abre,
    string? Cierra,
    bool TodoElDia,
    string? Notas);

public sealed record CentroOrganizacionDto(
    Guid Id,
    Guid OrganizacionId,
    string Nombre,
    string TipoCodigo,
    string TipoNombre,
    Guid TipoCentroId,
    Guid ZonaId,
    string ZonaNombre,
    string Direccion,
    string Telefono,
    string? Email,
    double? Latitud,
    double? Longitud,
    string EstadoVerificacion,
    string EstadoOperativo,
    string? MotivoEstado,
    string? Descripcion,
    IReadOnlyList<HorarioDto> Horarios,
    IReadOnlyDictionary<string, string> Recursos,
    DateTime? RecursosActualizadoAt);

public sealed record MiembroDto(
    Guid MembresiaId,
    Guid UsuarioId,
    string Nombre,
    string Email,
    string Rol,
    bool Activo,
    string? MotivoBaja,
    DateTime CreadoAt);

public sealed record PanelOrganizacionDto(
    OrganizacionDto Organizacion,
    string RolActor,
    int AsignacionesActivas,
    int AsignacionesCompletadas,
    int MiembrosActivos,
    IReadOnlyList<CentroOrganizacionDto> Centros);

public sealed record RegistrarOrganizacionRequest(
    Guid CiudadId,
    string Nombre,
    string Tipo,
    string EmailContacto,
    string TelefonoContacto,
    string? Descripcion);

/// <summary>
/// Horario del centro. Con <see cref="TodoElDia"/> el centro atiende las 24
/// horas todos los días; si no, se carga un turno para los días elegidos.
/// Sin días ni horas queda "horario no informado".
/// </summary>
public sealed record HorarioCentroRequest(
    bool TodoElDia,
    IReadOnlyList<int> Dias,
    TimeOnly? Abre,
    TimeOnly? Cierra);

public sealed record GuardarCentroRequest(
    string Nombre,
    Guid TipoCentroId,
    Guid ZonaId,
    string Direccion,
    string Telefono,
    string? Email,
    double? Latitud,
    double? Longitud,
    string? Descripcion,
    HorarioCentroRequest Horario);

/// <summary>
/// Reglas de las organizaciones: alta, centros, recursos declarados y miembros.
///
/// Permisos: cualquier miembro activo ve y opera; solo un responsable crea o
/// edita centros y administra miembros. La base protege la integridad
/// (último responsable de una comunitaria, unicidad de nombres).
/// </summary>
public sealed class OrganizacionAppService : ServicioBase
{
    public const int LargoMaximoNombre = 150;
    public const int LargoMaximoDescripcion = 500;
    public const int LargoMaximoMotivoBaja = 300;

    private static readonly string[] TiposOrganizacion = ["comunitaria", "institucional"];

    public OrganizacionAppService(
        AyudaYaDbContext db,
        UserManager<UsuarioIdentity> userManager)
        : base(db, userManager)
    {
    }

    // ------------------------------------------------------------------
    // Membresías y alta de la organización
    // ------------------------------------------------------------------

    /// <summary>Organizaciones donde la persona es miembro activo; primero donde es responsable.</summary>
    public Task<List<MembresiaDto>> ObtenerMembresiasAsync(
        Guid usuarioId, CancellationToken cancellationToken = default) =>
        Db.OrganizacionUsuario.AsNoTracking()
            .Where(ou => ou.UsuarioId == usuarioId && ou.Activo)
            .OrderByDescending(ou => ou.Rol == "responsable")
            .ThenBy(ou => ou.Organizacion.Nombre)
            .Select(ou => new MembresiaDto(
                ou.OrganizacionId,
                ou.Organizacion.Nombre,
                ou.Organizacion.Estado,
                ou.Rol))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Registra una organización nueva (queda pendiente hasta que un
    /// administrador la apruebe) y deja a quien la registra como responsable.
    /// </summary>
    public async Task<Resultado<Guid>> RegistrarAsync(
        Guid actorId,
        RegistrarOrganizacionRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var actor = await ObtenerActorActivoAsync(actorId);
        if (actor is null)
        {
            return Resultado<Guid>.Falla([UsuarioInvalido()]);
        }

        var errores = new List<ErrorNegocio>();

        if (!actor.EmailConfirmed)
        {
            errores.Add(Error(
                "EmailNoConfirmado",
                "Tenés que confirmar tu email antes de registrar una organización."));
        }

        var nombre = Limpiar(solicitud.Nombre);
        if (nombre is null)
        {
            errores.Add(Error("NombreRequerido", "El nombre de la organización es obligatorio."));
        }
        else if (nombre.Length > LargoMaximoNombre)
        {
            errores.Add(Error(
                "NombreMuyLargo",
                $"El nombre no puede superar los {LargoMaximoNombre} caracteres."));
        }

        var tipo = (solicitud.Tipo ?? string.Empty).Trim().ToLowerInvariant();
        if (!TiposOrganizacion.Contains(tipo))
        {
            errores.Add(Error("TipoInvalido", "Elegí si es una organización comunitaria o institucional."));
        }

        var email = NormalizarEmailContacto(solicitud.EmailContacto);
        if (email is null)
        {
            errores.Add(Error("EmailInvalido", "Escribí un correo de contacto válido."));
        }

        if (!ReglasNegocio.TelefonoValido(solicitud.TelefonoContacto, out var telefono))
        {
            errores.Add(Error(
                "TelefonoInvalido",
                "Escribí un teléfono válido, con característica (entre 8 y 13 dígitos)."));
        }

        var descripcion = Limpiar(solicitud.Descripcion);
        if (descripcion is { Length: > LargoMaximoDescripcion })
        {
            errores.Add(Error(
                "DescripcionMuyLarga",
                $"La descripción no puede superar los {LargoMaximoDescripcion} caracteres."));
        }

        if (errores.Count > 0)
        {
            return Resultado<Guid>.Falla(errores);
        }

        var ciudad = await Db.Ciudad.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == solicitud.CiudadId, cancellationToken);
        if (ciudad is null || !ciudad.Activa)
        {
            return Resultado<Guid>.Falla("CiudadInvalida", "La ciudad elegida no está disponible.");
        }

        var yaResponsable = await Db.OrganizacionUsuario.AsNoTracking().AnyAsync(
            ou => ou.UsuarioId == actorId && ou.Rol == "responsable" && ou.Activo,
            cancellationToken);
        if (yaResponsable)
        {
            return Resultado<Guid>.Falla(
                "YaEsResponsable",
                "Ya sos responsable de otra organización. Una persona solo puede ser responsable de una.");
        }

        var nombreNormalizado = nombre!.ToLower();
        var nombreRepetido = await Db.Organizacion.AsNoTracking().AnyAsync(
            o => o.CiudadId == solicitud.CiudadId && o.Nombre.ToLower() == nombreNormalizado,
            cancellationToken);
        if (nombreRepetido)
        {
            return Resultado<Guid>.Falla(
                "NombreDuplicado", "Ya existe una organización con ese nombre en la ciudad.");
        }

        var ahora = DateTime.UtcNow;
        var organizacion = new Organizacion
        {
            Id = Guid.NewGuid(),
            CiudadId = solicitud.CiudadId,
            Nombre = nombre,
            Tipo = tipo,
            Estado = "pendiente",
            EmailContacto = email!,
            TelefonoContacto = telefono,
            Descripcion = descripcion,
            RegistradaPor = actorId,
            CreadoAt = ahora,
            ActualizadoAt = ahora
        };

        organizacion.OrganizacionUsuario.Add(new OrganizacionUsuario
        {
            Id = Guid.NewGuid(),
            UsuarioId = actorId,
            Rol = "responsable",
            Activo = true,
            CreadoPor = actorId,
            CreadoAt = ahora,
            ActualizadoAt = ahora
        });

        Db.Organizacion.Add(organizacion);

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Resultado<Guid>.Falla(
                Codigos.ConflictoDeEstado, "No se pudo registrar la organización. Volvé a intentar.");
        }

        return Resultado<Guid>.Ok(organizacion.Id);
    }

    // ------------------------------------------------------------------
    // Panel
    // ------------------------------------------------------------------

    public async Task<Resultado<PanelOrganizacionDto>> ObtenerPanelAsync(
        Guid actorId, Guid organizacionId, CancellationToken cancellationToken = default)
    {
        var acceso = await VerificarMiembroAsync(actorId, organizacionId, cancellationToken);
        if (acceso.Error is not null)
        {
            return Resultado<PanelOrganizacionDto>.Falla([acceso.Error]);
        }

        var organizacion = await ObtenerOrganizacionDtoAsync(organizacionId, cancellationToken);
        if (organizacion is null)
        {
            return Resultado<PanelOrganizacionDto>.Falla(
                "OrganizacionNoEncontrada", "La organización no existe.");
        }

        var activas = await Db.Asignacion.AsNoTracking().CountAsync(
            a => a.OrganizacionId == organizacionId
                && (a.Estado == "asignada" || a.Estado == "en_atencion"),
            cancellationToken);
        var porConfirmar = await Db.Asignacion.AsNoTracking().CountAsync(
            a => a.OrganizacionId == organizacionId
                && a.Estado == "completada"
                && a.ConfirmadaAt == null,
            cancellationToken);
        var miembros = await Db.OrganizacionUsuario.AsNoTracking().CountAsync(
            ou => ou.OrganizacionId == organizacionId && ou.Activo,
            cancellationToken);

        var centros = await ConsultarCentros(c => c.OrganizacionId == organizacionId, cancellationToken);

        return Resultado<PanelOrganizacionDto>.Ok(new PanelOrganizacionDto(
            organizacion, acceso.Rol!, activas, porConfirmar, miembros, centros));
    }

    public async Task<Resultado<OrganizacionDto>> ObtenerOrganizacionAsync(
        Guid actorId, Guid organizacionId, CancellationToken cancellationToken = default)
    {
        var acceso = await VerificarMiembroAsync(actorId, organizacionId, cancellationToken);
        if (acceso.Error is not null)
        {
            return Resultado<OrganizacionDto>.Falla([acceso.Error]);
        }

        var dto = await ObtenerOrganizacionDtoAsync(organizacionId, cancellationToken);
        return dto is null
            ? Resultado<OrganizacionDto>.Falla("OrganizacionNoEncontrada", "La organización no existe.")
            : Resultado<OrganizacionDto>.Ok(dto);
    }

    // ------------------------------------------------------------------
    // Centros
    // ------------------------------------------------------------------

    public async Task<Resultado<List<CentroOrganizacionDto>>> ListarCentrosAsync(
        Guid actorId, Guid organizacionId, CancellationToken cancellationToken = default)
    {
        var acceso = await VerificarMiembroAsync(actorId, organizacionId, cancellationToken);
        if (acceso.Error is not null)
        {
            return Resultado<List<CentroOrganizacionDto>>.Falla([acceso.Error]);
        }

        return Resultado<List<CentroOrganizacionDto>>.Ok(
            await ConsultarCentros(c => c.OrganizacionId == organizacionId, cancellationToken));
    }

    public async Task<Resultado<CentroOrganizacionDto>> ObtenerCentroAsync(
        Guid actorId, Guid centroId, CancellationToken cancellationToken = default)
    {
        var organizacionId = await Db.Centro.AsNoTracking()
            .Where(c => c.Id == centroId)
            .Select(c => (Guid?)c.OrganizacionId)
            .SingleOrDefaultAsync(cancellationToken);

        if (organizacionId is null)
        {
            return Resultado<CentroOrganizacionDto>.Falla("CentroNoEncontrado", "El centro no existe.");
        }

        var acceso = await VerificarMiembroAsync(actorId, organizacionId.Value, cancellationToken);
        if (acceso.Error is not null)
        {
            return Resultado<CentroOrganizacionDto>.Falla([acceso.Error]);
        }

        var centros = await ConsultarCentros(c => c.Id == centroId, cancellationToken);
        return centros.Count == 0
            ? Resultado<CentroOrganizacionDto>.Falla("CentroNoEncontrado", "El centro no existe.")
            : Resultado<CentroOrganizacionDto>.Ok(centros[0]);
    }

    public async Task<Resultado<Guid>> CrearCentroAsync(
        Guid actorId,
        Guid organizacionId,
        GuardarCentroRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        var acceso = await VerificarResponsableAsync(actorId, organizacionId, cancellationToken);
        if (acceso is not null)
        {
            return Resultado<Guid>.Falla([acceso]);
        }

        var organizacion = await Db.Organizacion.AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == organizacionId, cancellationToken);
        if (organizacion is null)
        {
            return Resultado<Guid>.Falla("OrganizacionNoEncontrada", "La organización no existe.");
        }

        if (organizacion.Estado is not ("pendiente" or "activa"))
        {
            return Resultado<Guid>.Falla(
                "OrganizacionInactiva", "La organización no puede cargar centros en su estado actual.");
        }

        var validado = await ValidarCentroAsync(solicitud, organizacion.CiudadId, null, organizacionId, cancellationToken);
        if (validado.Errores.Count > 0)
        {
            return Resultado<Guid>.Falla(validado.Errores);
        }

        var ahora = DateTime.UtcNow;
        var centro = new Centro
        {
            Id = Guid.NewGuid(),
            OrganizacionId = organizacionId,
            CiudadId = organizacion.CiudadId,
            ZonaId = solicitud.ZonaId,
            TipoCentroId = solicitud.TipoCentroId,
            Nombre = validado.Nombre!,
            Direccion = validado.Direccion!,
            TelefonoContacto = validado.Telefono!,
            EmailContacto = validado.Email,
            Latitud = solicitud.Latitud,
            Longitud = solicitud.Longitud,
            EstadoVerificacion = "pendiente",
            EstadoOperativo = "operativo",
            Descripcion = validado.Descripcion,
            CreadoPor = actorId,
            CreadoAt = ahora,
            ActualizadoAt = ahora
        };

        foreach (var horario in ConstruirHorarios(solicitud.Horario, actorId, ahora))
        {
            centro.CentroHorario.Add(horario);
        }

        Db.Centro.Add(centro);

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Resultado<Guid>.Falla(
                Codigos.ConflictoDeEstado, "No se pudo guardar el centro. Volvé a intentar.");
        }

        return Resultado<Guid>.Ok(centro.Id);
    }

    /// <summary>
    /// Edita los datos del centro. Si estaba aprobado y cambia algo que lo
    /// identifica (nombre, tipo, zona, dirección o ubicación), vuelve a
    /// pendiente para que un administrador lo revise de nuevo. Si estaba
    /// rechazado, guardar lo vuelve a presentar: queda pendiente y sin motivo.
    /// Devuelve true cuando el centro quedó pendiente por la edición.
    /// </summary>
    public async Task<Resultado<bool>> ActualizarCentroAsync(
        Guid actorId,
        Guid centroId,
        GuardarCentroRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        var centro = await Db.Centro
            .Include(c => c.Organizacion)
            .Include(c => c.CentroHorario)
            .SingleOrDefaultAsync(c => c.Id == centroId, cancellationToken);

        if (centro is null)
        {
            return Resultado<bool>.Falla("CentroNoEncontrado", "El centro no existe.");
        }

        var acceso = await VerificarResponsableAsync(actorId, centro.OrganizacionId, cancellationToken);
        if (acceso is not null)
        {
            return Resultado<bool>.Falla([acceso]);
        }

        if (centro.Organizacion.Estado is not ("pendiente" or "activa"))
        {
            return Resultado<bool>.Falla(
                "OrganizacionInactiva", "La organización no puede editar centros en su estado actual.");
        }

        var validado = await ValidarCentroAsync(
            solicitud, centro.CiudadId, centro.Id, centro.OrganizacionId, cancellationToken);
        if (validado.Errores.Count > 0)
        {
            return Resultado<bool>.Falla(validado.Errores);
        }

        var cambiaIdentidad =
            !string.Equals(centro.Nombre, validado.Nombre, StringComparison.Ordinal)
            || centro.TipoCentroId != solicitud.TipoCentroId
            || centro.ZonaId != solicitud.ZonaId
            || !string.Equals(centro.Direccion, validado.Direccion, StringComparison.Ordinal)
            || centro.Latitud != solicitud.Latitud
            || centro.Longitud != solicitud.Longitud;

        var ahora = DateTime.UtcNow;
        centro.Nombre = validado.Nombre!;
        centro.TipoCentroId = solicitud.TipoCentroId;
        centro.ZonaId = solicitud.ZonaId;
        centro.Direccion = validado.Direccion!;
        centro.TelefonoContacto = validado.Telefono!;
        centro.EmailContacto = validado.Email;
        centro.Latitud = solicitud.Latitud;
        centro.Longitud = solicitud.Longitud;
        centro.Descripcion = validado.Descripcion;
        centro.ActualizadoAt = ahora;

        var volvioAPendiente = false;
        if (cambiaIdentidad && centro.EstadoVerificacion == "aprobado")
        {
            centro.EstadoVerificacion = "pendiente";
            volvioAPendiente = true;
        }
        else if (centro.EstadoVerificacion == "rechazado")
        {
            // Un rechazado se corrige editándolo: al guardar se vuelve a
            // presentar. La base no admite motivo en un centro pendiente.
            centro.EstadoVerificacion = "pendiente";
            centro.MotivoEstado = null;
            volvioAPendiente = true;
        }

        Db.CentroHorario.RemoveRange(centro.CentroHorario);
        foreach (var horario in ConstruirHorarios(solicitud.Horario, actorId, ahora))
        {
            horario.CentroId = centro.Id;
            Db.CentroHorario.Add(horario);
        }

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Resultado<bool>.Falla(
                Codigos.ConflictoDeEstado, "No se pudo guardar el centro. Volvé a intentar.");
        }

        return Resultado<bool>.Ok(volvioAPendiente);
    }

    /// <summary>
    /// Actualiza el estado operativo y la disponibilidad por recurso. Cada
    /// guardado renueva la fecha de actualización, así quien mira el mapa sabe
    /// cuán fresca es la información. Cualquier miembro activo puede hacerlo.
    /// </summary>
    public async Task<ResultadoOperacion> ActualizarRecursosAsync(
        Guid actorId,
        Guid centroId,
        string estadoOperativo,
        IReadOnlyDictionary<string, string?> niveles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(niveles);

        var centro = await Db.Centro
            .Include(c => c.Organizacion)
            .Include(c => c.CentroRecurso)
            .SingleOrDefaultAsync(c => c.Id == centroId, cancellationToken);

        if (centro is null)
        {
            return ResultadoOperacion.Falla(Error("CentroNoEncontrado", "El centro no existe."));
        }

        var acceso = await VerificarMiembroAsync(actorId, centro.OrganizacionId, cancellationToken);
        if (acceso.Error is not null)
        {
            return ResultadoOperacion.Falla(acceso.Error);
        }

        if (centro.Organizacion.Estado is not ("pendiente" or "activa"))
        {
            return ResultadoOperacion.Falla(Error(
                "OrganizacionInactiva", "La organización no puede actualizar recursos en su estado actual."));
        }

        var estado = (estadoOperativo ?? string.Empty).Trim().ToLowerInvariant();
        if (!ReglasNegocio.EstadosOperativos.Contains(estado))
        {
            return ResultadoOperacion.Falla(Error(
                "EstadoOperativoInvalido", "El estado del centro no es válido."));
        }

        var tipos = await Db.TipoRecurso.AsNoTracking()
            .Where(t => t.Activo)
            .ToDictionaryAsync(t => t.Codigo, t => t.Id, cancellationToken);

        foreach (var (codigo, nivel) in niveles)
        {
            if (!tipos.ContainsKey(codigo))
            {
                return ResultadoOperacion.Falla(Error(
                    "TipoRecursoInvalido", "Alguno de los recursos no existe o está inactivo."));
            }

            if (!string.IsNullOrEmpty(nivel)
                && nivel != "no_ofrece"
                && !ReglasNegocio.NivelesRecurso.Contains(nivel))
            {
                return ResultadoOperacion.Falla(Error(
                    "NivelInvalido", "Alguno de los niveles de disponibilidad no es válido."));
            }
        }

        var ahora = DateTime.UtcNow;
        centro.EstadoOperativo = estado;
        centro.ActualizadoAt = ahora;

        foreach (var (codigo, nivel) in niveles)
        {
            var tipoId = tipos[codigo];
            var existente = centro.CentroRecurso.FirstOrDefault(r => r.TipoRecursoId == tipoId);

            if (string.IsNullOrEmpty(nivel) || nivel == "no_ofrece")
            {
                if (existente is not null)
                {
                    Db.CentroRecurso.Remove(existente);
                }

                continue;
            }

            if (existente is null)
            {
                Db.CentroRecurso.Add(new CentroRecurso
                {
                    CentroId = centro.Id,
                    TipoRecursoId = tipoId,
                    Nivel = nivel,
                    ActualizadoPor = actorId,
                    ActualizadoAt = ahora,
                    CreadoAt = ahora
                });
            }
            else
            {
                existente.Nivel = nivel;
                existente.ActualizadoPor = actorId;
                existente.ActualizadoAt = ahora;
            }
        }

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Falla(Error(
                Codigos.ConflictoDeEstado, "No se pudo guardar la disponibilidad. Volvé a intentar."));
        }

        return ResultadoOperacion.Ok();
    }

    // ------------------------------------------------------------------
    // Miembros
    // ------------------------------------------------------------------

    public async Task<Resultado<List<MiembroDto>>> ListarMiembrosAsync(
        Guid actorId, Guid organizacionId, CancellationToken cancellationToken = default)
    {
        var acceso = await VerificarMiembroAsync(actorId, organizacionId, cancellationToken);
        if (acceso.Error is not null)
        {
            return Resultado<List<MiembroDto>>.Falla([acceso.Error]);
        }

        var miembros = await Db.OrganizacionUsuario.AsNoTracking()
            .Where(ou => ou.OrganizacionId == organizacionId)
            .OrderByDescending(ou => ou.Activo)
            .ThenByDescending(ou => ou.Rol == "responsable")
            .ThenBy(ou => ou.Usuario.NombreCompleto)
            .Select(ou => new MiembroDto(
                ou.Id,
                ou.UsuarioId,
                ou.Usuario.NombreCompleto,
                ou.Usuario.Email ?? string.Empty,
                ou.Rol,
                ou.Activo,
                ou.MotivoBaja,
                ou.CreadoAt))
            .ToListAsync(cancellationToken);

        return Resultado<List<MiembroDto>>.Ok(miembros);
    }

    /// <summary>Suma a una persona con cuenta (por email) como miembro o responsable.</summary>
    public async Task<ResultadoOperacion> AgregarMiembroAsync(
        Guid actorId,
        Guid organizacionId,
        string email,
        string rol,
        CancellationToken cancellationToken = default)
    {
        var acceso = await VerificarResponsableAsync(actorId, organizacionId, cancellationToken);
        if (acceso is not null)
        {
            return ResultadoOperacion.Falla(acceso);
        }

        rol = (rol ?? string.Empty).Trim().ToLowerInvariant();
        if (rol is not ("miembro" or "responsable"))
        {
            return ResultadoOperacion.Falla(Error("RolInvalido", "El rol tiene que ser miembro o responsable."));
        }

        var emailNormalizado = NormalizarEmailContacto(email);
        if (emailNormalizado is null)
        {
            return ResultadoOperacion.Falla(Error("EmailInvalido", "Escribí un correo electrónico válido."));
        }

        var organizacion = await Db.Organizacion.AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == organizacionId, cancellationToken);
        if (organizacion is null || organizacion.Estado is not ("pendiente" or "activa"))
        {
            return ResultadoOperacion.Falla(Error(
                "OrganizacionInactiva", "La organización no puede sumar personas en su estado actual."));
        }

        var usuario = await UserManager.FindByEmailAsync(emailNormalizado);
        if (usuario is null || !usuario.Activo)
        {
            return ResultadoOperacion.Falla(Error(
                "UsuarioNoEncontrado", "No hay una cuenta activa con ese correo. La persona tiene que registrarse primero."));
        }

        if (rol == "responsable"
            && await EsResponsableEnOtraOrganizacionAsync(usuario.Id, organizacionId, cancellationToken))
        {
            return ResultadoOperacion.Falla(Error(
                "YaEsResponsable", "Esa persona ya es responsable de otra organización."));
        }

        var existente = await Db.OrganizacionUsuario
            .SingleOrDefaultAsync(
                ou => ou.OrganizacionId == organizacionId && ou.UsuarioId == usuario.Id,
                cancellationToken);

        var ahora = DateTime.UtcNow;
        if (existente is { Activo: true })
        {
            return ResultadoOperacion.Falla(Error(
                "YaEsMiembro", "Esa persona ya es parte de la organización."));
        }

        if (existente is null)
        {
            Db.OrganizacionUsuario.Add(new OrganizacionUsuario
            {
                Id = Guid.NewGuid(),
                OrganizacionId = organizacionId,
                UsuarioId = usuario.Id,
                Rol = rol,
                Activo = true,
                CreadoPor = actorId,
                CreadoAt = ahora,
                ActualizadoAt = ahora
            });
        }
        else
        {
            existente.Rol = rol;
            existente.Activo = true;
            existente.DesactivadoAt = null;
            existente.MotivoBaja = null;
            existente.ActualizadoAt = ahora;
        }

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Falla(Error(
                Codigos.ConflictoDeEstado, "No se pudo sumar a la persona. Volvé a intentar."));
        }

        return ResultadoOperacion.Ok();
    }

    /// <summary>Baja lógica con motivo. No se borra a nadie: queda con fecha y motivo.</summary>
    public async Task<ResultadoOperacion> DarDeBajaMiembroAsync(
        Guid actorId,
        Guid organizacionId,
        Guid membresiaId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var acceso = await VerificarResponsableAsync(actorId, organizacionId, cancellationToken);
        if (acceso is not null)
        {
            return ResultadoOperacion.Falla(acceso);
        }

        var motivoLimpio = Limpiar(motivo);
        if (motivoLimpio is null)
        {
            return ResultadoOperacion.Falla(Error("MotivoRequerido", "Indicá el motivo de la baja."));
        }

        if (motivoLimpio.Length > LargoMaximoMotivoBaja)
        {
            return ResultadoOperacion.Falla(Error(
                "MotivoMuyLargo",
                $"El motivo no puede superar los {LargoMaximoMotivoBaja} caracteres."));
        }

        var membresia = await Db.OrganizacionUsuario
            .Include(ou => ou.Organizacion)
            .SingleOrDefaultAsync(
                ou => ou.Id == membresiaId && ou.OrganizacionId == organizacionId,
                cancellationToken);

        if (membresia is null || !membresia.Activo)
        {
            return ResultadoOperacion.Falla(Error(
                "MiembroNoEncontrado", "La persona no es un miembro activo de la organización."));
        }

        if (membresia.Rol == "responsable"
            && membresia.Organizacion.Tipo == "comunitaria"
            && !await HayOtroResponsableActivoAsync(organizacionId, membresia.Id, cancellationToken))
        {
            return ResultadoOperacion.Falla(Error(
                "UltimoResponsable",
                "Es la única persona responsable y la organización es comunitaria. Primero sumá a otro responsable."));
        }

        var ahora = DateTime.UtcNow;
        membresia.Activo = false;
        membresia.DesactivadoAt = ahora;
        membresia.MotivoBaja = motivoLimpio;
        membresia.ActualizadoAt = ahora;

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Falla(Error(
                Codigos.ConflictoDeEstado, "No se pudo dar de baja. Volvé a intentar."));
        }

        return ResultadoOperacion.Ok();
    }

    // ------------------------------------------------------------------
    // Ayudantes
    // ------------------------------------------------------------------

    private sealed record AccesoMiembro(string? Rol, ErrorNegocio? Error);

    private async Task<AccesoMiembro> VerificarMiembroAsync(
        Guid actorId, Guid organizacionId, CancellationToken cancellationToken)
    {
        var actor = await ObtenerActorActivoAsync(actorId);
        if (actor is null)
        {
            return new AccesoMiembro(null, UsuarioInvalido());
        }

        var rol = await Db.OrganizacionUsuario.AsNoTracking()
            .Where(ou => ou.OrganizacionId == organizacionId && ou.UsuarioId == actorId && ou.Activo)
            .Select(ou => ou.Rol)
            .SingleOrDefaultAsync(cancellationToken);

        return rol is null
            ? new AccesoMiembro(null, NoAutorizado("No sos miembro de esta organización."))
            : new AccesoMiembro(rol, null);
    }

    private async Task<ErrorNegocio?> VerificarResponsableAsync(
        Guid actorId, Guid organizacionId, CancellationToken cancellationToken)
    {
        var acceso = await VerificarMiembroAsync(actorId, organizacionId, cancellationToken);
        if (acceso.Error is not null)
        {
            return acceso.Error;
        }

        return acceso.Rol == "responsable"
            ? null
            : NoAutorizado("Solo una persona responsable puede hacer esto.");
    }

    private Task<bool> EsResponsableEnOtraOrganizacionAsync(
        Guid usuarioId, Guid organizacionId, CancellationToken cancellationToken) =>
        Db.OrganizacionUsuario.AsNoTracking().AnyAsync(
            ou => ou.UsuarioId == usuarioId
                && ou.Rol == "responsable"
                && ou.Activo
                && ou.OrganizacionId != organizacionId,
            cancellationToken);

    private Task<bool> HayOtroResponsableActivoAsync(
        Guid organizacionId, Guid membresiaExcluidaId, CancellationToken cancellationToken) =>
        Db.OrganizacionUsuario.AsNoTracking().AnyAsync(
            ou => ou.OrganizacionId == organizacionId
                && ou.Id != membresiaExcluidaId
                && ou.Rol == "responsable"
                && ou.Activo
                && ou.Usuario.Activo,
            cancellationToken);

    private Task<OrganizacionDto?> ObtenerOrganizacionDtoAsync(
        Guid organizacionId, CancellationToken cancellationToken) =>
        Db.Organizacion.AsNoTracking()
            .Where(o => o.Id == organizacionId)
            .Select(o => new OrganizacionDto(
                o.Id,
                o.Nombre,
                o.Tipo,
                o.Estado,
                o.MotivoEstado,
                o.CiudadId,
                o.Ciudad.Nombre,
                o.EmailContacto,
                o.TelefonoContacto,
                o.Descripcion,
                o.AprobadaAt))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<List<CentroOrganizacionDto>> ConsultarCentros(
        System.Linq.Expressions.Expression<Func<Centro, bool>> filtro,
        CancellationToken cancellationToken)
    {
        var filas = await Db.Centro.AsNoTracking()
            .Where(filtro)
            .OrderBy(c => c.Nombre)
            .Select(c => new
            {
                c.Id,
                c.OrganizacionId,
                c.Nombre,
                TipoCodigo = c.TipoCentro.Codigo,
                TipoNombre = c.TipoCentro.Nombre,
                c.TipoCentroId,
                c.ZonaId,
                ZonaNombre = c.Zona.Nombre,
                c.Direccion,
                c.TelefonoContacto,
                c.EmailContacto,
                c.Latitud,
                c.Longitud,
                c.EstadoVerificacion,
                c.EstadoOperativo,
                c.MotivoEstado,
                c.Descripcion,
                Horarios = c.CentroHorario
                    .OrderBy(h => h.DiaSemana).ThenBy(h => h.Abre)
                    .Select(h => new { h.DiaSemana, h.Abre, h.Cierra, h.TodoElDia, h.Notas })
                    .ToList(),
                Recursos = c.CentroRecurso
                    .Select(r => new { Codigo = r.TipoRecurso.Codigo, r.Nivel, r.ActualizadoAt })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return filas.Select(f => new CentroOrganizacionDto(
            f.Id,
            f.OrganizacionId,
            f.Nombre,
            f.TipoCodigo,
            f.TipoNombre,
            f.TipoCentroId,
            f.ZonaId,
            f.ZonaNombre,
            f.Direccion,
            f.TelefonoContacto,
            f.EmailContacto,
            f.Latitud,
            f.Longitud,
            f.EstadoVerificacion,
            f.EstadoOperativo,
            f.MotivoEstado,
            f.Descripcion,
            f.Horarios.Select(h => new HorarioDto(
                h.DiaSemana,
                h.Abre?.ToString("HH:mm"),
                h.Cierra?.ToString("HH:mm"),
                h.TodoElDia,
                h.Notas)).ToList(),
            f.Recursos.ToDictionary(r => r.Codigo, r => r.Nivel),
            f.Recursos.Count == 0 ? null : f.Recursos.Max(r => r.ActualizadoAt))).ToList();
    }

    private sealed record CentroValidado(
        IReadOnlyList<ErrorNegocio> Errores,
        string? Nombre,
        string? Direccion,
        string? Telefono,
        string? Email,
        string? Descripcion);

    private async Task<CentroValidado> ValidarCentroAsync(
        GuardarCentroRequest solicitud,
        Guid ciudadId,
        Guid? centroActualId,
        Guid organizacionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var errores = new List<ErrorNegocio>();

        var nombre = Limpiar(solicitud.Nombre);
        if (nombre is null)
        {
            errores.Add(Error("NombreRequerido", "Ingresá el nombre del centro."));
        }
        else if (nombre.Length > 100)
        {
            errores.Add(Error("NombreMuyLargo", "El nombre del centro no puede superar los 100 caracteres."));
        }

        var direccion = Limpiar(solicitud.Direccion);
        if (direccion is null)
        {
            errores.Add(Error("DireccionRequerida", "Ingresá la dirección."));
        }
        else if (direccion.Length > 200)
        {
            errores.Add(Error("DireccionMuyLarga", "La dirección no puede superar los 200 caracteres."));
        }

        if (!ReglasNegocio.TelefonoValido(solicitud.Telefono, out var telefono))
        {
            errores.Add(Error(
                "TelefonoInvalido",
                "Escribí un teléfono válido, con característica (entre 8 y 13 dígitos)."));
        }

        string? email = null;
        if (!string.IsNullOrWhiteSpace(solicitud.Email))
        {
            email = NormalizarEmailContacto(solicitud.Email);
            if (email is null)
            {
                errores.Add(Error("EmailInvalido", "El correo del centro no es válido."));
            }
        }

        var descripcion = Limpiar(solicitud.Descripcion);
        if (descripcion is { Length: > 300 })
        {
            errores.Add(Error("DescripcionMuyLarga", "La descripción no puede superar los 300 caracteres."));
        }

        if ((solicitud.Latitud is null) != (solicitud.Longitud is null)
            || (solicitud.Latitud is not null && !ReglasNegocio.CoordenadasValidas(solicitud.Latitud, solicitud.Longitud)))
        {
            errores.Add(Error(
                "CoordenadasInvalidas",
                "La ubicación en el mapa no es válida. Elegí un punto en el mapa o dejala vacía."));
        }

        var horario = solicitud.Horario;
        if (horario is not null && !horario.TodoElDia)
        {
            var hayDatos = horario.Dias.Count > 0 || horario.Abre is not null || horario.Cierra is not null;
            if (hayDatos)
            {
                if (horario.Dias.Count == 0)
                {
                    errores.Add(Error("HorarioSinDias", "Marcá los días de atención."));
                }

                if (horario.Dias.Any(d => d is < 1 or > 7))
                {
                    errores.Add(Error("HorarioDiaInvalido", "Alguno de los días no es válido."));
                }

                if (horario.Abre is null || horario.Cierra is null)
                {
                    errores.Add(Error("HorarioIncompleto", "Ingresá la hora de apertura y la de cierre."));
                }
                else if (horario.Abre >= horario.Cierra)
                {
                    errores.Add(Error(
                        "HorarioInvalido", "La hora de cierre tiene que ser posterior a la de apertura."));
                }
            }
        }

        if (errores.Count > 0)
        {
            return new CentroValidado(errores, null, null, null, null, null);
        }

        var tipoValido = await Db.TipoCentro.AsNoTracking()
            .AnyAsync(t => t.Id == solicitud.TipoCentroId && t.Activo, cancellationToken);
        if (!tipoValido)
        {
            errores.Add(Error("TipoCentroInvalido", "Elegí un tipo de centro válido."));
        }

        var zonaValida = await Db.Zona.AsNoTracking().AnyAsync(
            z => z.Id == solicitud.ZonaId && z.CiudadId == ciudadId && z.Activa,
            cancellationToken);
        if (!zonaValida)
        {
            errores.Add(Error("ZonaInvalida", "La zona no pertenece a la ciudad de la organización o no está activa."));
        }

        var nombreMinuscula = nombre!.ToLower();
        var estadoDelRepetido = await Db.Centro.AsNoTracking()
            .Where(c => c.OrganizacionId == organizacionId
                && c.Id != centroActualId
                && c.Nombre.ToLower() == nombreMinuscula)
            .Select(c => c.EstadoVerificacion)
            .FirstOrDefaultAsync(cancellationToken);
        if (estadoDelRepetido is not null)
        {
            // El índice único ux_centro_nombre incluye a los rechazados: para
            // volver a presentar uno se lo edita en lugar de crearlo de nuevo.
            errores.Add(Error("NombreDuplicado", estadoDelRepetido == "rechazado"
                ? "Ya tienen un centro rechazado con ese nombre. Para volver a presentarlo, editalo desde Centros."
                : "Ya tienen un centro con ese nombre."));
        }

        if (await ExisteOtroCentroEnElLugarAsync(
                ciudadId, solicitud.TipoCentroId, direccion!, centroActualId, cancellationToken))
        {
            errores.Add(CentroYaRegistrado(
                "Ya hay un centro de ese tipo registrado en esa dirección. Si es el mismo lugar, no hace falta cargarlo de nuevo."));
        }

        return new CentroValidado(errores, nombre, direccion, telefono, email, descripcion);
    }

    private static IEnumerable<CentroHorario> ConstruirHorarios(
        HorarioCentroRequest? horario, Guid actorId, DateTime ahora)
    {
        if (horario is null)
        {
            yield break;
        }

        if (horario.TodoElDia)
        {
            for (short dia = 1; dia <= 7; dia++)
            {
                yield return new CentroHorario
                {
                    Id = Guid.NewGuid(),
                    DiaSemana = dia,
                    TodoElDia = true,
                    ActualizadoPor = actorId,
                    CreadoAt = ahora,
                    ActualizadoAt = ahora
                };
            }

            yield break;
        }

        if (horario.Abre is null || horario.Cierra is null)
        {
            yield break;
        }

        foreach (var dia in horario.Dias.Distinct().Order())
        {
            yield return new CentroHorario
            {
                Id = Guid.NewGuid(),
                DiaSemana = (short)dia,
                Abre = horario.Abre,
                Cierra = horario.Cierra,
                TodoElDia = false,
                ActualizadoPor = actorId,
                CreadoAt = ahora,
                ActualizadoAt = ahora
            };
        }
    }

    /// <summary>Correo en minúsculas y con formato válido; null si no sirve.</summary>
    internal static string? NormalizarEmailContacto(string? email)
    {
        var limpio = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (limpio.Length is 0 or > 254)
        {
            return null;
        }

        return MailAddress.TryCreate(limpio, out var direccion) && direccion.Address == limpio && limpio.Contains('.')
            ? limpio
            : null;
    }
}

using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Negocio.Servicios;

public sealed record RecursoResumenDto(
    string Codigo,
    string Nombre,
    int Personas,
    string? Detalle,
    string Estado);

public sealed record SolicitudResumenDto(
    Guid Id,
    string Codigo,
    string Estado,
    string Urgencia,
    string ZonaNombre,
    string CiudadNombre,
    int PersonasAfectadas,
    string? Descripcion,
    DateTime CreadoAt,
    DateTime ActualizadoAt,
    IReadOnlyList<RecursoResumenDto> Recursos)
{
    public bool EstaAbierta => Estado is "pendiente" or "asignada" or "en_atencion" or "parcial";
}

public sealed record AsignadoDto(
    string Tipo,
    string Nombre,
    string Estado,
    string? TelefonoContacto);

public sealed record RecursoSolicitudDetalleDto(
    Guid Id,
    string TipoCodigo,
    string TipoNombre,
    int Personas,
    string? Detalle,
    string Estado,
    AsignadoDto? Asignado,
    Guid? AsignacionId,
    bool PuedeConfirmar);

public sealed record HistorialSolicitudDto(
    string? EstadoAnterior,
    string EstadoNuevo,
    string Actor,
    bool EsElSolicitante,
    string? Motivo,
    DateTime CreadoAt);

public sealed record ContactoSolicitudDto(
    string Direccion,
    string Telefono,
    double? Latitud,
    double? Longitud);

public sealed record SolicitudDetalleDto(
    Guid Id,
    string Codigo,
    string Estado,
    string Urgencia,
    string ZonaNombre,
    string CiudadNombre,
    int PersonasAfectadas,
    string? Descripcion,
    string? MotivoCancelacion,
    DateTime CreadoAt,
    DateTime ActualizadoAt,
    string SolicitanteNombre,
    bool EsDelSolicitante,
    ContactoSolicitudDto? Contacto,
    IReadOnlyList<RecursoSolicitudDetalleDto> Recursos,
    IReadOnlyList<HistorialSolicitudDto> Historial)
{
    public bool EstaAbierta => Estado is "pendiente" or "asignada" or "en_atencion" or "parcial";
}

public sealed record RecursoDisponibleDto(
    Guid Id,
    string TipoCodigo,
    string TipoNombre,
    int Personas,
    string? Detalle);

public sealed record SolicitudDisponibleDto(
    Guid Id,
    string Codigo,
    string Urgencia,
    string Estado,
    Guid ZonaId,
    string ZonaNombre,
    string CiudadNombre,
    int PersonasAfectadas,
    string? Descripcion,
    DateTime CreadoAt,
    IReadOnlyList<RecursoDisponibleDto> RecursosPendientes,
    IReadOnlyList<string> RecursosYaTomados);

/// <summary>
/// Consultas de solicitudes para la persona que pidió ayuda, para el
/// administrador y para quienes buscan solicitudes para tomar.
/// </summary>
public sealed class SolicitudConsultasAppService : ServicioBase
{
    public const int MaximoDisponibles = 50;

    public SolicitudConsultasAppService(
        AyudaYaDbContext db,
        UserManager<UsuarioIdentity> userManager)
        : base(db, userManager)
    {
    }

    public async Task<List<SolicitudResumenDto>> ListarMisSolicitudesAsync(
        Guid solicitanteId, CancellationToken cancellationToken = default)
    {
        var filas = await Db.Solicitud.AsNoTracking()
            .Where(s => s.SolicitanteId == solicitanteId)
            .OrderByDescending(s => s.CreadoAt)
            .Select(s => new
            {
                s.Id,
                s.Estado,
                s.Urgencia,
                ZonaNombre = s.Zona.Nombre,
                CiudadNombre = s.Ciudad.Nombre,
                s.PersonasAfectadas,
                s.Descripcion,
                s.CreadoAt,
                s.ActualizadoAt,
                Recursos = s.SolicitudRecurso
                    .OrderBy(r => r.TipoRecurso.Nombre)
                    .Select(r => new RecursoResumenDto(
                        r.TipoRecurso.Codigo,
                        r.TipoRecurso.Nombre,
                        r.Personas,
                        r.Detalle,
                        r.Estado))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return filas.Select(f => new SolicitudResumenDto(
            f.Id,
            ReglasNegocio.CodigoCorto(f.Id),
            f.Estado,
            f.Urgencia,
            f.ZonaNombre,
            f.CiudadNombre,
            f.PersonasAfectadas,
            f.Descripcion,
            f.CreadoAt,
            f.ActualizadoAt,
            f.Recursos)).ToList();
    }

    /// <summary>
    /// Detalle completo de una solicitud. Solo lo ven la persona que la creó
    /// y un administrador. Quienes la atienden usan el detalle de su asignación.
    /// </summary>
    public async Task<Resultado<SolicitudDetalleDto>> ObtenerDetalleAsync(
        Guid actorId, Guid solicitudId, CancellationToken cancellationToken = default)
    {
        var actor = await ObtenerActorActivoAsync(actorId);
        if (actor is null)
        {
            return Resultado<SolicitudDetalleDto>.Falla([UsuarioInvalido()]);
        }

        var s = await Db.Solicitud.AsNoTracking()
            .Where(x => x.Id == solicitudId)
            .Select(x => new
            {
                x.Id,
                x.SolicitanteId,
                SolicitanteNombre = x.Solicitante.NombreCompleto,
                x.Estado,
                x.Urgencia,
                ZonaNombre = x.Zona.Nombre,
                CiudadNombre = x.Ciudad.Nombre,
                x.PersonasAfectadas,
                x.Descripcion,
                x.MotivoCancelacion,
                x.CreadoAt,
                x.ActualizadoAt,
                Contacto = x.SolicitudContacto == null
                    ? null
                    : new ContactoSolicitudDto(
                        x.SolicitudContacto.Direccion,
                        x.SolicitudContacto.Telefono,
                        x.SolicitudContacto.Latitud,
                        x.SolicitudContacto.Longitud)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (s is null)
        {
            return Resultado<SolicitudDetalleDto>.Falla(
                "SolicitudNoEncontrada", "La solicitud no existe.");
        }

        var esAdmin = await EsAdministradorAsync(actor);
        var esSolicitante = s.SolicitanteId == actorId;
        if (!esAdmin && !esSolicitante)
        {
            return Resultado<SolicitudDetalleDto>.Falla(
                Codigos.NoAutorizado, "No tenés permiso para ver esta solicitud.");
        }

        var recursosFilas = await Db.SolicitudRecurso.AsNoTracking()
            .Where(r => r.SolicitudId == solicitudId)
            .OrderBy(r => r.TipoRecurso.Nombre)
            .Select(r => new
            {
                r.Id,
                TipoCodigo = r.TipoRecurso.Codigo,
                TipoNombre = r.TipoRecurso.Nombre,
                r.Personas,
                r.Detalle,
                r.Estado,
                Asignacion = r.Asignaciones
                    .Where(a => a.Estado == "asignada"
                        || a.Estado == "en_atencion"
                        || a.Estado == "completada")
                    .Select(a => new
                    {
                        a.Id,
                        a.Estado,
                        a.ConfirmadaAt,
                        Voluntario = a.Voluntario == null ? null : a.Voluntario.Usuario.NombreCompleto,
                        VoluntarioTelefono = a.Voluntario == null ? null : a.Voluntario.TelefonoContacto,
                        Organizacion = a.Organizacion == null ? null : a.Organizacion.Nombre,
                        OrganizacionTelefono = a.Organizacion == null ? null : a.Organizacion.TelefonoContacto
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var recursos = recursosFilas.Select(r =>
        {
            AsignadoDto? asignado = null;
            if (r.Asignacion is not null)
            {
                var activa = r.Asignacion.Estado is "asignada" or "en_atencion";
                asignado = r.Asignacion.Organizacion is not null
                    ? new AsignadoDto(
                        "organizacion",
                        r.Asignacion.Organizacion,
                        r.Asignacion.Estado,
                        activa ? r.Asignacion.OrganizacionTelefono : null)
                    : new AsignadoDto(
                        "voluntario",
                        ReglasNegocio.NombreCorto(r.Asignacion.Voluntario),
                        r.Asignacion.Estado,
                        activa ? r.Asignacion.VoluntarioTelefono : null);
            }

            var puedeConfirmar = r.Asignacion is { Estado: "completada", ConfirmadaAt: null };
            return new RecursoSolicitudDetalleDto(
                r.Id, r.TipoCodigo, r.TipoNombre, r.Personas, r.Detalle, r.Estado,
                asignado, r.Asignacion?.Id, puedeConfirmar);
        }).ToList();

        var historialFilas = await Db.HistorialSolicitud.AsNoTracking()
            .Where(h => h.SolicitudId == solicitudId)
            .OrderBy(h => h.CreadoAt)
            .Select(h => new
            {
                h.EstadoAnterior,
                h.EstadoNuevo,
                h.ActorId,
                Actor = h.Actor.NombreCompleto,
                h.Motivo,
                h.CreadoAt
            })
            .ToListAsync(cancellationToken);

        var historial = historialFilas.Select(h => new HistorialSolicitudDto(
            h.EstadoAnterior,
            h.EstadoNuevo,
            ReglasNegocio.NombreCorto(h.Actor),
            h.ActorId == s.SolicitanteId,
            h.Motivo,
            h.CreadoAt)).ToList();

        return Resultado<SolicitudDetalleDto>.Ok(new SolicitudDetalleDto(
            s.Id,
            ReglasNegocio.CodigoCorto(s.Id),
            s.Estado,
            s.Urgencia,
            s.ZonaNombre,
            s.CiudadNombre,
            s.PersonasAfectadas,
            s.Descripcion,
            s.MotivoCancelacion,
            s.CreadoAt,
            s.ActualizadoAt,
            s.SolicitanteNombre,
            esSolicitante,
            s.Contacto,
            recursos,
            historial));
    }

    /// <summary>
    /// Solicitudes con al menos un recurso pendiente en una ciudad (y zona,
    /// si se indica), con las críticas primero. No incluye dirección,
    /// teléfono ni coordenadas: eso solo se ve al tomar el recurso.
    /// Con <paramref name="tipoRecurso"/> solo entran las que tienen pendiente
    /// ese recurso; con <paramref name="urgencia"/>, las de esa urgencia.
    /// </summary>
    public async Task<List<SolicitudDisponibleDto>> ListarDisponiblesAsync(
        Guid actorId,
        Guid ciudadId,
        Guid? zonaId,
        string? tipoRecurso = null,
        string? urgencia = null,
        CancellationToken cancellationToken = default)
    {
        var filas = await Db.Solicitud.AsNoTracking()
            .Where(s => s.CiudadId == ciudadId
                && (zonaId == null || s.ZonaId == zonaId)
                && (urgencia == null || s.Urgencia == urgencia)
                && s.SolicitanteId != actorId
                && (s.Estado == "pendiente"
                    || s.Estado == "asignada"
                    || s.Estado == "en_atencion"
                    || s.Estado == "parcial")
                && s.SolicitudRecurso.Any(r => r.Estado == "pendiente"
                    && (tipoRecurso == null || r.TipoRecurso.Codigo == tipoRecurso)))
            .OrderByDescending(s => s.Urgencia == "critica")
            .ThenByDescending(s => s.CreadoAt)
            .Take(MaximoDisponibles)
            .Select(s => new
            {
                s.Id,
                s.Urgencia,
                s.Estado,
                s.ZonaId,
                ZonaNombre = s.Zona.Nombre,
                CiudadNombre = s.Ciudad.Nombre,
                s.PersonasAfectadas,
                s.Descripcion,
                s.CreadoAt,
                Recursos = s.SolicitudRecurso
                    .OrderBy(r => r.TipoRecurso.Nombre)
                    .Select(r => new
                    {
                        r.Id,
                        r.Estado,
                        TipoCodigo = r.TipoRecurso.Codigo,
                        TipoNombre = r.TipoRecurso.Nombre,
                        r.Personas,
                        r.Detalle
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return filas.Select(f => new SolicitudDisponibleDto(
            f.Id,
            ReglasNegocio.CodigoCorto(f.Id),
            f.Urgencia,
            f.Estado,
            f.ZonaId,
            f.ZonaNombre,
            f.CiudadNombre,
            f.PersonasAfectadas,
            f.Descripcion,
            f.CreadoAt,
            f.Recursos
                .Where(r => r.Estado == "pendiente")
                .Select(r => new RecursoDisponibleDto(
                    r.Id, r.TipoCodigo, r.TipoNombre, r.Personas, r.Detalle))
                .ToList(),
            f.Recursos
                .Where(r => r.Estado is "asignada" or "en_atencion" or "resuelta")
                .Select(r => r.TipoNombre)
                .ToList())).ToList();
    }

    /// <summary>Listado administrativo de todas las solicitudes, con filtro por estado.</summary>
    public async Task<Resultado<List<SolicitudResumenDto>>> ListarTodasAsync(
        Guid adminId,
        string? estado,
        int limite = 100,
        CancellationToken cancellationToken = default)
    {
        var actor = await ObtenerActorActivoAsync(adminId);
        if (actor is null || !await EsAdministradorAsync(actor))
        {
            return Resultado<List<SolicitudResumenDto>>.Falla(
                Codigos.NoAutorizado, "Solo un administrador puede ver todas las solicitudes.");
        }

        var consulta = Db.Solicitud.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            consulta = consulta.Where(s => s.Estado == estado);
        }

        var filas = await consulta
            .OrderByDescending(s => s.CreadoAt)
            .Take(Math.Clamp(limite, 1, 500))
            .Select(s => new
            {
                s.Id,
                s.Estado,
                s.Urgencia,
                ZonaNombre = s.Zona.Nombre,
                CiudadNombre = s.Ciudad.Nombre,
                s.PersonasAfectadas,
                s.CreadoAt,
                s.ActualizadoAt,
                Recursos = s.SolicitudRecurso
                    .OrderBy(r => r.TipoRecurso.Nombre)
                    .Select(r => new RecursoResumenDto(
                        r.TipoRecurso.Codigo, r.TipoRecurso.Nombre, r.Personas, null, r.Estado))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Resultado<List<SolicitudResumenDto>>.Ok(filas.Select(f => new SolicitudResumenDto(
            f.Id,
            ReglasNegocio.CodigoCorto(f.Id),
            f.Estado,
            f.Urgencia,
            f.ZonaNombre,
            f.CiudadNombre,
            f.PersonasAfectadas,
            null,
            f.CreadoAt,
            f.ActualizadoAt,
            f.Recursos)).ToList());
    }
}

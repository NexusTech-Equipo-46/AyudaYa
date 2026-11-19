using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AyudaYa.Negocio.Servicios;

public sealed record AsignacionResumenDto(
    Guid Id,
    string Estado,
    string RecursoCodigo,
    string RecursoNombre,
    int Personas,
    string? DetalleRecurso,
    Guid SolicitudId,
    string SolicitudCodigo,
    string Urgencia,
    string ZonaNombre,
    string CiudadNombre,
    int PersonasAfectadas,
    string SolicitanteNombre,
    DateTime AsignadaAt,
    string AsignadaPor,
    string? DestinatarioNombre)
{
    /// <summary>Asignada o en atención: hay algo para hacer.</summary>
    public bool EsActiva => Estado is "asignada" or "en_atencion";

    /// <summary>Completada y todavía sin confirmar por quien pidió ayuda.</summary>
    public bool EsPorConfirmar => Estado == "completada";

    public bool EsCritica => Urgencia == "critica";
}

public sealed record ContactoAsignacionDto(
    string Nombre,
    string Direccion,
    string Telefono,
    double? Latitud,
    double? Longitud);

public sealed record EventoAsignacionDto(
    string Estado,
    string Actor,
    DateTime Cuando,
    string? Motivo);

public sealed record AsignacionDetalleDto(
    Guid Id,
    string Estado,
    string RecursoCodigo,
    string RecursoNombre,
    int Personas,
    string? DetalleRecurso,
    Guid SolicitudId,
    string SolicitudCodigo,
    string SolicitudEstado,
    string Urgencia,
    string ZonaNombre,
    string CiudadNombre,
    int PersonasAfectadas,
    string? DescripcionSolicitud,
    string SolicitanteNombre,
    string DestinatarioTipo,
    string DestinatarioNombre,
    Guid? OrganizacionId,
    DateTime AsignadaAt,
    string AsignadaPor,
    ContactoAsignacionDto? Contacto,
    bool PuedeIniciar,
    bool PuedeCompletar,
    bool PuedeLiberar,
    IReadOnlyList<EventoAsignacionDto> Linea);

/// <summary>
/// Ciclo de vida de una asignación: iniciar, completar, confirmar y liberar,
/// más las consultas de quien atiende (voluntario u organización).
///
/// Cada acción actualiza la asignación, el recurso solicitado y el estado de
/// la solicitud dentro de la misma transacción, y registra el historial.
/// </summary>
public sealed class AsignacionAppService : ServicioBase
{
    public const int LargoMaximoMotivo = 300;

    private readonly NotificacionesAppService _notificaciones;

    public AsignacionAppService(
        AyudaYaDbContext db,
        UserManager<UsuarioIdentity> userManager,
        NotificacionesAppService notificaciones)
        : base(db, userManager)
    {
        _notificaciones = notificaciones;
    }

    // ------------------------------------------------------------------
    // Consultas
    // ------------------------------------------------------------------

    public Task<List<AsignacionResumenDto>> ListarDeVoluntarioAsync(
        Guid voluntarioId, CancellationToken cancellationToken = default) =>
        ListarAsync(a => a.VoluntarioId == voluntarioId, cancellationToken);

    /// <summary>Asignaciones de la organización. Solo las ve un miembro activo o un administrador.</summary>
    public async Task<Resultado<List<AsignacionResumenDto>>> ListarDeOrganizacionAsync(
        Guid actorId, Guid organizacionId, CancellationToken cancellationToken = default)
    {
        var actor = await ObtenerActorActivoAsync(actorId);
        if (actor is null)
        {
            return Resultado<List<AsignacionResumenDto>>.Falla([UsuarioInvalido()]);
        }

        if (!await EsAdministradorAsync(actor)
            && !await EsMiembroActivoAsync(actorId, organizacionId, cancellationToken))
        {
            return Resultado<List<AsignacionResumenDto>>.Falla(
                Codigos.NoAutorizado, "Solo los miembros de la organización ven sus asignaciones.");
        }

        return Resultado<List<AsignacionResumenDto>>.Ok(
            await ListarAsync(a => a.OrganizacionId == organizacionId, cancellationToken));
    }

    private async Task<List<AsignacionResumenDto>> ListarAsync(
        System.Linq.Expressions.Expression<Func<Asignacion, bool>> filtro,
        CancellationToken cancellationToken)
    {
        var filas = await Db.Asignacion.AsNoTracking()
            .Where(filtro)
            .OrderByDescending(a => a.AsignadaAt)
            .Select(a => new
            {
                a.Id,
                a.Estado,
                a.ConfirmadaAt,
                RecursoCodigo = a.SolicitudRecurso.TipoRecurso.Codigo,
                RecursoNombre = a.SolicitudRecurso.TipoRecurso.Nombre,
                a.SolicitudRecurso.Personas,
                a.SolicitudRecurso.Detalle,
                SolicitudId = a.SolicitudRecurso.SolicitudId,
                a.SolicitudRecurso.Solicitud.Urgencia,
                ZonaNombre = a.SolicitudRecurso.Solicitud.Zona.Nombre,
                CiudadNombre = a.SolicitudRecurso.Solicitud.Ciudad.Nombre,
                a.SolicitudRecurso.Solicitud.PersonasAfectadas,
                Solicitante = a.SolicitudRecurso.Solicitud.Solicitante.NombreCompleto,
                a.AsignadaAt,
                AsignadaPor = a.AsignadaPorNavigation.NombreCompleto,
                Voluntario = a.Voluntario == null ? null : a.Voluntario.Usuario.NombreCompleto,
                Organizacion = a.Organizacion == null ? null : a.Organizacion.Nombre
            })
            .ToListAsync(cancellationToken);

        return filas.Select(f => new AsignacionResumenDto(
            f.Id,
            ReglasNegocio.EstadoVisibleAsignacion(f.Estado, f.ConfirmadaAt),
            f.RecursoCodigo,
            f.RecursoNombre,
            f.Personas,
            f.Detalle,
            f.SolicitudId,
            ReglasNegocio.CodigoCorto(f.SolicitudId),
            f.Urgencia,
            f.ZonaNombre,
            f.CiudadNombre,
            f.PersonasAfectadas,
            ReglasNegocio.NombreCorto(f.Solicitante),
            f.AsignadaAt,
            ReglasNegocio.NombreCorto(f.AsignadaPor),
            f.Organizacion ?? ReglasNegocio.NombreCorto(f.Voluntario)))
            .OrderBy(a => a.EsActiva ? 0 : a.EsPorConfirmar ? 1 : 2)
            .ThenByDescending(a => a.EsActiva && a.EsCritica)
            .ThenByDescending(a => a.AsignadaAt)
            .ToList();
    }

    /// <summary>
    /// Detalle de una asignación para quien la atiende (el voluntario o un
    /// miembro activo de la organización) o un administrador. La dirección y
    /// el teléfono solo se incluyen mientras la regla de acceso lo permite.
    /// </summary>
    public async Task<Resultado<AsignacionDetalleDto>> ObtenerAsync(
        Guid actorId, Guid asignacionId, CancellationToken cancellationToken = default)
    {
        var actor = await ObtenerActorActivoAsync(actorId);
        if (actor is null)
        {
            return Resultado<AsignacionDetalleDto>.Falla([UsuarioInvalido()]);
        }

        var a = await Db.Asignacion.AsNoTracking()
            .Where(x => x.Id == asignacionId)
            .Select(x => new
            {
                x.Id,
                x.Estado,
                x.VoluntarioId,
                x.OrganizacionId,
                OrganizacionEstado = x.Organizacion == null ? null : x.Organizacion.Estado,
                x.AsignadaAt,
                AsignadaPor = x.AsignadaPorNavigation.NombreCompleto,
                IniciadaPor = x.IniciadaPorNavigation == null ? null : x.IniciadaPorNavigation.NombreCompleto,
                x.IniciadaAt,
                CompletadaPor = x.CompletadaPorNavigation == null ? null : x.CompletadaPorNavigation.NombreCompleto,
                x.CompletadaAt,
                ConfirmadaPor = x.ConfirmadaPorNavigation == null ? null : x.ConfirmadaPorNavigation.NombreCompleto,
                x.ConfirmadaAt,
                CanceladaPor = x.CanceladaPorNavigation == null ? null : x.CanceladaPorNavigation.NombreCompleto,
                x.CanceladaAt,
                x.MotivoCancelacion,
                LiberadaPor = x.LiberadaPorNavigation == null ? null : x.LiberadaPorNavigation.NombreCompleto,
                x.LiberadaAt,
                x.MotivoLiberacion,
                RecursoCodigo = x.SolicitudRecurso.TipoRecurso.Codigo,
                RecursoNombre = x.SolicitudRecurso.TipoRecurso.Nombre,
                x.SolicitudRecurso.Personas,
                x.SolicitudRecurso.Detalle,
                SolicitudId = x.SolicitudRecurso.SolicitudId,
                SolicitudEstado = x.SolicitudRecurso.Solicitud.Estado,
                x.SolicitudRecurso.Solicitud.Urgencia,
                ZonaNombre = x.SolicitudRecurso.Solicitud.Zona.Nombre,
                CiudadNombre = x.SolicitudRecurso.Solicitud.Ciudad.Nombre,
                x.SolicitudRecurso.Solicitud.PersonasAfectadas,
                x.SolicitudRecurso.Solicitud.Descripcion,
                Solicitante = x.SolicitudRecurso.Solicitud.Solicitante.NombreCompleto,
                Contacto = x.SolicitudRecurso.Solicitud.SolicitudContacto,
                Voluntario = x.Voluntario == null ? null : x.Voluntario.Usuario.NombreCompleto,
                Organizacion = x.Organizacion == null ? null : x.Organizacion.Nombre
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (a is null)
        {
            return Resultado<AsignacionDetalleDto>.Falla(
                "AsignacionNoEncontrada", "La asignación no existe.");
        }

        var esAdmin = await EsAdministradorAsync(actor);
        var esDestinatario = a.VoluntarioId == actorId
            || (a.OrganizacionId.HasValue
                && await EsMiembroActivoAsync(actorId, a.OrganizacionId.Value, cancellationToken));

        if (!esAdmin && !esDestinatario)
        {
            return Resultado<AsignacionDetalleDto>.Falla(
                Codigos.NoAutorizado, "No tenés permiso para ver esta asignación.");
        }

        var organizacionOperativa = a.OrganizacionEstado is null or "activa";
        ContactoAsignacionDto? contacto = null;
        if ((esAdmin || organizacionOperativa)
            && ReglasNegocio.AsignacionDaAccesoAlContacto(a.Estado, a.ConfirmadaAt)
            && a.Contacto is not null)
        {
            contacto = new ContactoAsignacionDto(
                a.Solicitante,
                a.Contacto.Direccion,
                a.Contacto.Telefono,
                a.Contacto.Latitud,
                a.Contacto.Longitud);
        }

        var solicitudCerrada = a.SolicitudEstado is "resuelta" or "cancelada";
        var puedeOperar = esDestinatario && organizacionOperativa && !solicitudCerrada;

        var linea = new List<EventoAsignacionDto>
        {
            new("asignada", ReglasNegocio.NombreCorto(a.AsignadaPor), a.AsignadaAt, null)
        };
        if (a.IniciadaAt is not null)
        {
            linea.Add(new("en_atencion", ReglasNegocio.NombreCorto(a.IniciadaPor), a.IniciadaAt.Value, null));
        }

        if (a.CompletadaAt is not null)
        {
            linea.Add(new("completada", ReglasNegocio.NombreCorto(a.CompletadaPor), a.CompletadaAt.Value, null));
        }

        if (a.ConfirmadaAt is not null)
        {
            linea.Add(new("confirmada", ReglasNegocio.NombreCorto(a.ConfirmadaPor), a.ConfirmadaAt.Value, null));
        }

        if (a.CanceladaAt is not null)
        {
            linea.Add(new("cancelada", ReglasNegocio.NombreCorto(a.CanceladaPor), a.CanceladaAt.Value, a.MotivoCancelacion));
        }

        if (a.LiberadaAt is not null)
        {
            linea.Add(new("liberada", ReglasNegocio.NombreCorto(a.LiberadaPor), a.LiberadaAt.Value, a.MotivoLiberacion));
        }

        return Resultado<AsignacionDetalleDto>.Ok(new AsignacionDetalleDto(
            a.Id,
            ReglasNegocio.EstadoVisibleAsignacion(a.Estado, a.ConfirmadaAt),
            a.RecursoCodigo,
            a.RecursoNombre,
            a.Personas,
            a.Detalle,
            a.SolicitudId,
            ReglasNegocio.CodigoCorto(a.SolicitudId),
            a.SolicitudEstado,
            a.Urgencia,
            a.ZonaNombre,
            a.CiudadNombre,
            a.PersonasAfectadas,
            a.Descripcion,
            ReglasNegocio.NombreCorto(a.Solicitante),
            a.Organizacion is not null ? "organizacion" : "voluntario",
            a.Organizacion ?? ReglasNegocio.NombreCorto(a.Voluntario),
            a.OrganizacionId,
            a.AsignadaAt,
            ReglasNegocio.NombreCorto(a.AsignadaPor),
            contacto,
            puedeOperar && a.Estado == "asignada",
            puedeOperar && a.Estado == "en_atencion",
            (esDestinatario || esAdmin) && !solicitudCerrada && a.Estado is "asignada" or "en_atencion",
            linea.OrderByDescending(e => e.Cuando).ToList()));
    }

    // ------------------------------------------------------------------
    // Acciones
    // ------------------------------------------------------------------

    /// <summary>De asignada a en atención. Lo hace quien atiende.</summary>
    public Task<ResultadoOperacion> IniciarAsync(
        Guid actorId, Guid asignacionId, CancellationToken cancellationToken = default) =>
        EjecutarAsync(actorId, asignacionId, Accion.Iniciar, null, cancellationToken);

    /// <summary>De en atención a completada. Lo hace quien atiende; falta la confirmación de quien pidió ayuda.</summary>
    public Task<ResultadoOperacion> CompletarAsync(
        Guid actorId, Guid asignacionId, CancellationToken cancellationToken = default) =>
        EjecutarAsync(actorId, asignacionId, Accion.Completar, null, cancellationToken);

    /// <summary>Quien pidió ayuda confirma que recibió el recurso: lo deja resuelto.</summary>
    public Task<ResultadoOperacion> ConfirmarAsync(
        Guid actorId, Guid asignacionId, CancellationToken cancellationToken = default) =>
        EjecutarAsync(actorId, asignacionId, Accion.Confirmar, null, cancellationToken);

    /// <summary>Quien atiende se retira: el recurso vuelve a quedar pendiente. Exige motivo.</summary>
    public Task<ResultadoOperacion> LiberarAsync(
        Guid actorId, Guid asignacionId, string motivo, CancellationToken cancellationToken = default) =>
        EjecutarAsync(actorId, asignacionId, Accion.Liberar, motivo, cancellationToken);

    private enum Accion { Iniciar, Completar, Confirmar, Liberar }

    private async Task<ResultadoOperacion> EjecutarAsync(
        Guid actorId,
        Guid asignacionId,
        Accion accion,
        string? motivo,
        CancellationToken cancellationToken)
    {
        string? motivoLimpio = null;
        if (accion == Accion.Liberar)
        {
            motivoLimpio = Limpiar(motivo);
            if (motivoLimpio is null)
            {
                return ResultadoOperacion.Falla(Error(
                    "MotivoRequerido", "Contanos por qué liberás la asignación."));
            }

            if (motivoLimpio.Length > LargoMaximoMotivo)
            {
                return ResultadoOperacion.Falla(Error(
                    "MotivoMuyLargo",
                    $"El motivo no puede superar los {LargoMaximoMotivo} caracteres."));
            }
        }

        var actor = await ObtenerActorActivoAsync(actorId);
        if (actor is null)
        {
            return ResultadoOperacion.Falla(UsuarioInvalido());
        }

        ResultadoOperacion resultado;
        await using (var transaccion = await Db.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                // Bloquea la fila antes de leer el estado. Así dos acciones simultáneas
                // sobre la misma asignación se serializan: la segunda espera y, al
                // continuar, ve el estado que confirmó la primera.
                await Db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM asignacion WHERE id = {asignacionId} FOR UPDATE",
                    cancellationToken);

                resultado = await EjecutarConBloqueoAsync(
                    actor, actorId, asignacionId, accion, motivoLimpio, transaccion, cancellationToken);
            }
            catch (DbUpdateException)
            {
                return ResultadoOperacion.Falla(Error(
                    Codigos.ConflictoDeEstado,
                    "La asignación cambió mientras se actualizaba. Actualizá los datos y volvé a intentar."));
            }
        }

        // Con la transacción ya confirmada, se avisa a quien pidió ayuda (mejor esfuerzo).
        if (resultado.Exito && accion == Accion.Completar)
        {
            await _notificaciones.AvisarRecursoCompletadoAsync(asignacionId, actorId, cancellationToken);
        }

        return resultado;
    }

    private async Task<ResultadoOperacion> EjecutarConBloqueoAsync(
        UsuarioIdentity actor,
        Guid actorId,
        Guid asignacionId,
        Accion accion,
        string? motivoLimpio,
        IDbContextTransaction transaccion,
        CancellationToken cancellationToken)
    {
        var asignacion = await Db.Asignacion
            .Include(a => a.Organizacion)
            .Include(a => a.SolicitudRecurso)
                .ThenInclude(r => r.Solicitud)
                    .ThenInclude(s => s.SolicitudRecurso)
            .SingleOrDefaultAsync(a => a.Id == asignacionId, cancellationToken);

        if (asignacion is null)
        {
            return ResultadoOperacion.Falla(Error(
                "AsignacionNoEncontrada", "La asignación no existe."));
        }

        var recurso = asignacion.SolicitudRecurso;
        var solicitud = recurso.Solicitud;
        var esAdmin = await EsAdministradorAsync(actor);

        // Quién puede actuar según la acción.
        if (accion == Accion.Confirmar)
        {
            if (!esAdmin && solicitud.SolicitanteId != actorId)
            {
                return ResultadoOperacion.Falla(NoAutorizado(
                    "Solo quien pidió ayuda puede confirmar que la recibió."));
            }
        }
        else
        {
            var esDestinatario = asignacion.VoluntarioId == actorId
                || (asignacion.OrganizacionId.HasValue
                    && await EsMiembroActivoAsync(actorId, asignacion.OrganizacionId.Value, cancellationToken));

            if (!esDestinatario && !esAdmin)
            {
                return ResultadoOperacion.Falla(NoAutorizado(
                    "Solo quien atiende esta asignación puede modificarla."));
            }
        }

        if (solicitud.Estado is "resuelta" or "cancelada")
        {
            return ResultadoOperacion.Falla(Error(
                "SolicitudCerrada", "La solicitud ya está cerrada."));
        }

        if (accion is Accion.Iniciar or Accion.Completar
            && asignacion.Organizacion is { Estado: not "activa" })
        {
            return ResultadoOperacion.Falla(Error(
                "OrganizacionInactiva", "La organización no está activa."));
        }

        var transicionValida = accion switch
        {
            Accion.Iniciar => asignacion.Estado == "asignada",
            Accion.Completar => asignacion.Estado == "en_atencion",
            Accion.Confirmar => asignacion.Estado == "completada" && asignacion.ConfirmadaAt is null,
            Accion.Liberar => asignacion.Estado is "asignada" or "en_atencion",
            _ => false
        };

        if (!transicionValida)
        {
            return ResultadoOperacion.Falla(Error(
                "TransicionInvalida",
                "Esa acción no corresponde al estado actual de la asignación."));
        }

        var ahora = DateTime.UtcNow;
        var estadoSolicitudAnterior = solicitud.Estado;
        string? motivoHistorial = null;

        switch (accion)
        {
            case Accion.Iniciar:
                asignacion.Estado = "en_atencion";
                asignacion.IniciadaPor = actorId;
                asignacion.IniciadaAt = ahora;
                asignacion.ActualizadoAt = ahora;
                recurso.Estado = "en_atencion";
                recurso.ActualizadoAt = ahora;
                motivoHistorial = "Se inició la atención de un recurso.";
                break;

            case Accion.Completar:
                asignacion.Estado = "completada";
                asignacion.CompletadaPor = actorId;
                asignacion.CompletadaAt = ahora;
                asignacion.ActualizadoAt = ahora;
                motivoHistorial = "Se completó la atención de un recurso. Falta la confirmación.";
                break;

            case Accion.Confirmar:
                asignacion.ConfirmadaPor = actorId;
                asignacion.ConfirmadaAt = ahora;
                asignacion.ActualizadoAt = ahora;
                recurso.Estado = "resuelta";
                recurso.ResueltaAt = ahora;
                recurso.ActualizadoAt = ahora;
                motivoHistorial = "Se confirmó la atención de un recurso.";
                break;

            case Accion.Liberar:
                asignacion.Estado = "liberada";
                asignacion.LiberadaPor = actorId;
                asignacion.LiberadaAt = ahora;
                asignacion.MotivoLiberacion = motivoLimpio;
                asignacion.ActualizadoAt = ahora;

                // La base no deja volver el recurso a pendiente mientras la
                // asignación siga viva: primero se guarda la liberación.
                await Db.SaveChangesAsync(cancellationToken);

                recurso.Estado = "pendiente";
                recurso.ResueltaAt = null;
                recurso.ActualizadoAt = ahora;
                motivoHistorial = $"Se liberó un recurso: {motivoLimpio}";
                break;
        }

        var nuevoEstado = ReglasNegocio.CalcularEstadoSolicitud(
            solicitud.SolicitudRecurso.Select(r => r.Estado));

        if (nuevoEstado != estadoSolicitudAnterior)
        {
            solicitud.Estado = nuevoEstado;
            solicitud.ActualizadoAt = ahora;

            Db.HistorialSolicitud.Add(new HistorialSolicitud
            {
                Id = Guid.NewGuid(),
                SolicitudId = solicitud.Id,
                EstadoAnterior = estadoSolicitudAnterior,
                EstadoNuevo = nuevoEstado,
                ActorId = actorId,
                Motivo = motivoHistorial,
                CreadoAt = ahora
            });
        }

        await Db.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);

        return ResultadoOperacion.Ok();
    }
}

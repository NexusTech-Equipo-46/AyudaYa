using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Negocio.Servicios;

/// <summary>
/// Error de negocio con un código estable para que la capa Web decida cómo
/// mostrarlo. No contiene datos de persistencia ni detalles internos.
/// </summary>
public sealed record ErrorNegocio(string Codigo, string Mensaje);

/// <summary>
/// Resultado de una operación que no devuelve datos (por ejemplo, asignar o
/// cancelar). Si <see cref="Exito"/> es false, <see cref="Errores"/> explica
/// el motivo con códigos pensados para la UI.
/// </summary>
public sealed record ResultadoOperacion(bool Exito, IReadOnlyList<ErrorNegocio> Errores)
{
    public static ResultadoOperacion Ok() => new(true, Array.Empty<ErrorNegocio>());

    public static ResultadoOperacion Falla(params ErrorNegocio[] errores) =>
        new(false, errores);
}

/// <summary>
/// Resultado de crear una solicitud. Devuelve el id generado para que la capa
/// Web pueda redirigir al detalle sin consultar de nuevo.
/// </summary>
public sealed record ResultadoCreacionSolicitud(
    Guid? SolicitudId,
    IReadOnlyList<ErrorNegocio> Errores)
{
    public bool Exito => SolicitudId.HasValue;

    public static ResultadoCreacionSolicitud Ok(Guid solicitudId) =>
        new(solicitudId, Array.Empty<ErrorNegocio>());

    public static ResultadoCreacionSolicitud Falla(params ErrorNegocio[] errores) =>
        new(null, errores);
}

/// <summary>
/// Datos de contacto y coordenadas exactas. Es información privada: solo se
/// entrega a quien autoriza <see cref="SolicitudAppService.ObtenerUbicacionExactaAsync"/>.
/// </summary>
public sealed record UbicacionExactaSolicitudDto(
    string Direccion,
    string Telefono,
    double Latitud,
    double Longitud);

/// <summary>Resultado de consultar la ubicación exacta de una solicitud.</summary>
public sealed record ResultadoUbicacionExacta(
    UbicacionExactaSolicitudDto? Ubicacion,
    IReadOnlyList<ErrorNegocio> Errores)
{
    public bool Exito => Ubicacion is not null;

    public static ResultadoUbicacionExacta Ok(UbicacionExactaSolicitudDto ubicacion) =>
        new(ubicacion, Array.Empty<ErrorNegocio>());

    public static ResultadoUbicacionExacta Falla(params ErrorNegocio[] errores) =>
        new(null, errores);
}

/// <summary>
/// Un recurso pedido dentro de una solicitud nueva. <paramref name="Personas"/>
/// es la cantidad de personas alcanzadas por ese recurso, no el total de la
/// solicitud.
/// </summary>
public sealed record RecursoSolicitadoRequest(
    Guid TipoRecursoId,
    int Personas,
    string? Detalle = null);

/// <summary>
/// Datos que la capa Web envía para crear una solicitud. Las coordenadas son
/// obligatorias: la base las permite nulas solo por compatibilidad histórica.
/// </summary>
public sealed record CrearSolicitudRequest(
    Guid CiudadId,
    Guid ZonaId,
    string Urgencia,
    int PersonasAfectadas,
    string? Descripcion,
    string Direccion,
    string Telefono,
    double? Latitud,
    double? Longitud,
    IReadOnlyList<RecursoSolicitadoRequest> Recursos);

/// <summary>
/// Reglas de negocio de las solicitudes: creación, asignación, cancelación y
/// acceso a la ubicación exacta.
///
/// La base protege invariantes estructurales (claves, estados válidos,
/// una asignación activa por recurso, hitos coherentes). Este servicio agrega
/// lo que la base no puede decidir: quién puede hacer cada acción, si el email
/// está confirmado y qué datos privados se pueden mostrar.
/// </summary>
public sealed class SolicitudAppService
{
    public const int LargoMaximoDescripcion = 500;
    public const int LargoMaximoDireccion = 200;
    public const int LargoMaximoTelefono = 20;
    public const int LargoMaximoDetalleRecurso = 200;
    public const int LargoMaximoMotivoCancelacion = 300;

    /// <summary>
    /// Estados que la base considera una asignación activa. Se usan para
    /// decidir quién puede ver la ubicación exacta y para bloquear una segunda
    /// asignación sobre el mismo recurso.
    /// </summary>
    private static readonly string[] EstadosAsignacionActiva =
        ["asignada", "en_atencion", "completada"];

    private static readonly string[] EstadosSolicitudCerrada =
        ["resuelta", "cancelada"];

    private static readonly string[] UrgenciasValidas =
        ["normal", "critica"];

    private readonly AyudaYaDbContext _db;
    private readonly UserManager<UsuarioIdentity> _userManager;

    public SolicitudAppService(
        AyudaYaDbContext db,
        UserManager<UsuarioIdentity> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>
    /// Crea una solicitud con su contacto, sus recursos y el primer registro
    /// de historial. Todo se guarda en un solo SaveChanges, así que la base
    /// confirma la solicitud completa o no guarda nada.
    ///
    /// Reglas principales:
    /// - el solicitante debe existir, estar activo y tener el email confirmado;
    /// - la zona debe existir, estar activa y pertenecer a la ciudad indicada;
    /// - dirección y teléfono son obligatorios;
    /// - las coordenadas exactas son obligatorias y deben estar en rango;
    /// - debe pedirse al menos un recurso activo, sin repetir tipos.
    /// </summary>
    public async Task<ResultadoCreacionSolicitud> CrearAsync(
        Guid solicitanteId,
        CrearSolicitudRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var solicitante = await _userManager.FindByIdAsync(solicitanteId.ToString());
        if (solicitante is null)
        {
            return ResultadoCreacionSolicitud.Falla(Error(
                "SolicitanteNoEncontrado",
                "El usuario no existe."));
        }

        if (!solicitante.Activo)
        {
            return ResultadoCreacionSolicitud.Falla(Error(
                "UsuarioInactivo",
                "La cuenta está inactiva y no puede crear solicitudes."));
        }

        if (!solicitante.EmailConfirmed)
        {
            return ResultadoCreacionSolicitud.Falla(Error(
                "EmailNoConfirmado",
                "Tenés que confirmar tu email antes de crear una solicitud."));
        }

        var errores = new List<ErrorNegocio>();

        var urgencia = (solicitud.Urgencia ?? string.Empty).Trim().ToLowerInvariant();
        if (!UrgenciasValidas.Contains(urgencia))
        {
            errores.Add(Error(
                "UrgenciaInvalida",
                "La urgencia debe ser normal o crítica."));
        }

        if (solicitud.PersonasAfectadas <= 0)
        {
            errores.Add(Error(
                "PersonasAfectadasInvalida",
                "La cantidad de personas afectadas debe ser mayor a cero."));
        }

        var descripcion = Limpiar(solicitud.Descripcion);
        if (descripcion is { Length: > LargoMaximoDescripcion })
        {
            errores.Add(Error(
                "DescripcionMuyLarga",
                $"La descripción no puede superar los {LargoMaximoDescripcion} caracteres."));
        }

        var direccion = Limpiar(solicitud.Direccion);
        if (direccion is null)
        {
            errores.Add(Error("DireccionRequerida", "La dirección exacta es obligatoria."));
        }
        else if (direccion.Length > LargoMaximoDireccion)
        {
            errores.Add(Error(
                "DireccionMuyLarga",
                $"La dirección no puede superar los {LargoMaximoDireccion} caracteres."));
        }

        var telefono = Limpiar(solicitud.Telefono);
        if (telefono is null)
        {
            errores.Add(Error("TelefonoRequerido", "El teléfono de contacto es obligatorio."));
        }
        else
        {
            if (telefono.Length > LargoMaximoTelefono)
            {
                errores.Add(Error(
                    "TelefonoMuyLargo",
                    $"El teléfono no puede superar los {LargoMaximoTelefono} caracteres."));
            }

            var digitos = telefono.Count(char.IsAsciiDigit);
            if (digitos is < 8 or > 13)
            {
                errores.Add(Error(
                    "TelefonoInvalido",
                    "El teléfono debe tener entre 8 y 13 dígitos, con característica."));
            }
        }

        var latitud = solicitud.Latitud;
        var longitud = solicitud.Longitud;
        if (latitud is null || longitud is null)
        {
            errores.Add(Error(
                "CoordenadasRequeridas",
                "La ubicación exacta es obligatoria para crear una solicitud."));
        }
        else if (!double.IsFinite(latitud.Value) || !double.IsFinite(longitud.Value))
        {
            errores.Add(Error(
                "CoordenadasInvalidas",
                "Las coordenadas deben ser números válidos."));
        }
        else if (latitud is < -90 or > 90 || longitud is < -180 or > 180)
        {
            errores.Add(Error(
                "CoordenadasFueraDeRango",
                "La latitud debe estar entre -90 y 90, y la longitud entre -180 y 180."));
        }

        var recursos = solicitud.Recursos ?? Array.Empty<RecursoSolicitadoRequest>();
        if (recursos.Count == 0)
        {
            errores.Add(Error(
                "RecursosRequeridos",
                "Tenés que pedir al menos un recurso."));
        }
        else
        {
            var tiposDuplicados = recursos
                .GroupBy(r => r.TipoRecursoId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (tiposDuplicados.Count > 0)
            {
                errores.Add(Error(
                    "RecursoDuplicado",
                    "No se puede pedir el mismo tipo de recurso dos veces."));
            }

            foreach (var recurso in recursos)
            {
                if (recurso.TipoRecursoId == Guid.Empty)
                {
                    errores.Add(Error(
                        "RecursoInvalido",
                        "Todos los recursos deben tener un tipo válido."));
                }

                if (recurso.Personas <= 0)
                {
                    errores.Add(Error(
                        "PersonasRecursoInvalida",
                        "Cada recurso debe indicar al menos una persona."));
                }
                else if (recurso.Personas > solicitud.PersonasAfectadas)
                {
                    errores.Add(Error(
                        "PersonasRecursoExcedeTotal",
                        "Las personas de cada recurso no pueden superar las personas afectadas."));
                }

                var detalle = Limpiar(recurso.Detalle);
                if (detalle is { Length: > LargoMaximoDetalleRecurso })
                {
                    errores.Add(Error(
                        "DetalleRecursoMuyLargo",
                        $"El detalle de cada recurso no puede superar los {LargoMaximoDetalleRecurso} caracteres."));
                }
            }
        }

        if (errores.Count > 0)
        {
            return ResultadoCreacionSolicitud.Falla(errores.ToArray());
        }

        var zona = await _db.Zona
            .AsNoTracking()
            .Where(z => z.Id == solicitud.ZonaId)
            .Select(z => new
            {
                z.CiudadId,
                z.Activa,
                CiudadActiva = z.Ciudad.Activa
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (zona is null)
        {
            return ResultadoCreacionSolicitud.Falla(Error(
                "ZonaNoEncontrada",
                "La zona indicada no existe."));
        }

        if (!zona.Activa || !zona.CiudadActiva)
        {
            return ResultadoCreacionSolicitud.Falla(Error(
                "ZonaInactiva",
                "La zona o su ciudad no están activas."));
        }

        if (zona.CiudadId != solicitud.CiudadId)
        {
            return ResultadoCreacionSolicitud.Falla(Error(
                "ZonaCiudadInvalida",
                "La zona no pertenece a la ciudad indicada."));
        }

        var tipoRecursoIds = recursos
            .Select(r => r.TipoRecursoId)
            .Distinct()
            .ToList();

        var tiposValidos = await _db.TipoRecurso
            .AsNoTracking()
            .Where(t => tipoRecursoIds.Contains(t.Id) && t.Activo)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (tipoRecursoIds.Except(tiposValidos).Any())
        {
            return ResultadoCreacionSolicitud.Falla(Error(
                "TipoRecursoInvalido",
                "Alguno de los recursos pedidos no existe o está inactivo."));
        }

        var ahora = DateTime.UtcNow;
        var solicitudId = Guid.NewGuid();

        _db.Solicitud.Add(new Solicitud
        {
            Id = solicitudId,
            SolicitanteId = solicitanteId,
            CiudadId = zona.CiudadId,
            ZonaId = solicitud.ZonaId,
            Urgencia = urgencia,
            Estado = "pendiente",
            PersonasAfectadas = solicitud.PersonasAfectadas,
            Descripcion = descripcion,
            CreadoAt = ahora,
            ActualizadoAt = ahora
        });

        _db.SolicitudContacto.Add(new SolicitudContacto
        {
            SolicitudId = solicitudId,
            Direccion = direccion!,
            Telefono = telefono!,
            Latitud = latitud,
            Longitud = longitud,
            CreadoAt = ahora,
            ActualizadoAt = ahora
        });

        foreach (var recurso in recursos)
        {
            _db.SolicitudRecurso.Add(new SolicitudRecurso
            {
                Id = Guid.NewGuid(),
                SolicitudId = solicitudId,
                TipoRecursoId = recurso.TipoRecursoId,
                Personas = recurso.Personas,
                Detalle = Limpiar(recurso.Detalle),
                Estado = "pendiente",
                CreadoAt = ahora,
                ActualizadoAt = ahora
            });
        }

        _db.HistorialSolicitud.Add(new HistorialSolicitud
        {
            Id = Guid.NewGuid(),
            SolicitudId = solicitudId,
            EstadoAnterior = null,
            EstadoNuevo = "pendiente",
            ActorId = solicitanteId,
            Motivo = "Solicitud creada.",
            CreadoAt = ahora
        });

        await _db.SaveChangesAsync(cancellationToken);

        return ResultadoCreacionSolicitud.Ok(solicitudId);
    }

    /// <summary>
    /// Devuelve dirección, teléfono y coordenadas exactas solo si el actor está
    /// autorizado. Pueden verlas:
    /// - el solicitante;
    /// - un administrador;
    /// - el voluntario con una asignación activa sobre la solicitud;
    /// - un miembro activo de una organización con una asignación activa.
    ///
    /// Una asignación cancelada o liberada ya no da acceso.
    /// </summary>
    public async Task<ResultadoUbicacionExacta> ObtenerUbicacionExactaAsync(
        Guid actorId,
        Guid solicitudId,
        CancellationToken cancellationToken = default)
    {
        var actor = await _userManager.FindByIdAsync(actorId.ToString());
        if (actor is null || !actor.Activo)
        {
            return ResultadoUbicacionExacta.Falla(Error(
                "UsuarioInvalido",
                "El usuario no existe o está inactivo."));
        }

        var solicitud = await _db.Solicitud
            .AsNoTracking()
            .Where(s => s.Id == solicitudId)
            .Select(s => new { s.SolicitanteId })
            .SingleOrDefaultAsync(cancellationToken);

        if (solicitud is null)
        {
            return ResultadoUbicacionExacta.Falla(Error(
                "SolicitudNoEncontrada",
                "La solicitud no existe."));
        }

        var esAdministrador = await _userManager.IsInRoleAsync(
            actor,
            UsuarioAppService.RolAdministrador);

        if (!esAdministrador && solicitud.SolicitanteId != actorId)
        {
            var tieneAsignacionActiva = await _db.Asignacion
                .AsNoTracking()
                .Where(a => a.SolicitudRecurso.SolicitudId == solicitudId
                    && EstadosAsignacionActiva.Contains(a.Estado))
                .AnyAsync(
                    a => a.VoluntarioId == actorId
                        || (a.OrganizacionId != null
                            && _db.OrganizacionUsuario.Any(ou =>
                                ou.OrganizacionId == a.OrganizacionId
                                && ou.UsuarioId == actorId
                                && ou.Activo)),
                    cancellationToken);

            if (!tieneAsignacionActiva)
            {
                return ResultadoUbicacionExacta.Falla(Error(
                    "NoAutorizado",
                    "No tenés permiso para ver la ubicación exacta de esta solicitud."));
            }
        }

        var contacto = await _db.SolicitudContacto
            .AsNoTracking()
            .Where(c => c.SolicitudId == solicitudId)
            .Select(c => new
            {
                c.Direccion,
                c.Telefono,
                c.Latitud,
                c.Longitud
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (contacto is null || contacto.Latitud is null || contacto.Longitud is null)
        {
            return ResultadoUbicacionExacta.Falla(Error(
                "UbicacionNoDisponible",
                "La solicitud no tiene datos de ubicación exacta registrados."));
        }

        return ResultadoUbicacionExacta.Ok(new UbicacionExactaSolicitudDto(
            contacto.Direccion,
            contacto.Telefono,
            contacto.Latitud.Value,
            contacto.Longitud.Value));
    }

    /// <summary>
    /// Asigna un recurso pendiente a un voluntario o a una organización.
    ///
    /// Reglas:
    /// - el recurso debe seguir pendiente y la solicitud no debe estar cerrada;
    /// - se indica exactamente un destinatario;
    /// - el voluntario debe estar activo y pertenecer a la ciudad (y a su zona,
    ///   si eligió una) de la solicitud;
    /// - la organización debe estar activa y ser de la misma ciudad;
    /// - un voluntario solo puede tomar el recurso para sí mismo, salvo que lo
    ///   haga un administrador;
    /// - una organización solo puede asignarse si el actor es miembro activo,
    ///   salvo que lo haga un administrador.
    ///
    /// La asignación y el cambio de estado se guardan en dos pasos dentro de
    /// una transacción porque los triggers de la base exigen que el recurso
    /// siga pendiente al insertar la asignación.
    /// </summary>
    public async Task<ResultadoOperacion> AsignarAsync(
        Guid actorId,
        Guid solicitudRecursoId,
        Guid? voluntarioId,
        Guid? organizacionId,
        CancellationToken cancellationToken = default)
    {
        if (voluntarioId.HasValue == organizacionId.HasValue)
        {
            return ResultadoOperacion.Falla(Error(
                "DestinatarioInvalido",
                "Tenés que indicar exactamente un voluntario o una organización."));
        }

        var actor = await _userManager.FindByIdAsync(actorId.ToString());
        if (actor is null || !actor.Activo)
        {
            return ResultadoOperacion.Falla(Error(
                "UsuarioInvalido",
                "El usuario no existe o está inactivo."));
        }

        var recurso = await _db.SolicitudRecurso
            .Include(sr => sr.Solicitud)
            .SingleOrDefaultAsync(sr => sr.Id == solicitudRecursoId, cancellationToken);

        if (recurso is null)
        {
            return ResultadoOperacion.Falla(Error(
                "RecursoNoEncontrado",
                "El recurso solicitado no existe."));
        }

        if (recurso.Estado != "pendiente")
        {
            return ResultadoOperacion.Falla(Error(
                "RecursoNoDisponible",
                "El recurso ya no está pendiente."));
        }

        if (EstadosSolicitudCerrada.Contains(recurso.Solicitud.Estado))
        {
            return ResultadoOperacion.Falla(Error(
                "SolicitudCerrada",
                "No se puede asignar un recurso de una solicitud cerrada."));
        }

        var existeAsignacionActiva = await _db.Asignacion
            .AsNoTracking()
            .AnyAsync(
                a => a.SolicitudRecursoId == solicitudRecursoId
                    && EstadosAsignacionActiva.Contains(a.Estado),
                cancellationToken);

        if (existeAsignacionActiva)
        {
            return ResultadoOperacion.Falla(Error(
                "AsignacionActiva",
                "El recurso ya tiene una asignación activa."));
        }

        var esAdministrador = await _userManager.IsInRoleAsync(
            actor,
            UsuarioAppService.RolAdministrador);

        if (voluntarioId.HasValue)
        {
            var voluntario = await _db.Voluntario
                .AsNoTracking()
                .Where(v => v.UsuarioId == voluntarioId.Value)
                .Select(v => new
                {
                    v.Activo,
                    v.CiudadId,
                    v.ZonaId,
                    UsuarioActivo = v.Usuario.Activo
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (voluntario is null)
            {
                return ResultadoOperacion.Falla(Error(
                    "VoluntarioNoEncontrado",
                    "El voluntario no existe."));
            }

            if (!voluntario.Activo || !voluntario.UsuarioActivo)
            {
                return ResultadoOperacion.Falla(Error(
                    "VoluntarioInactivo",
                    "El voluntario o su usuario están inactivos."));
            }

            if (voluntario.CiudadId != recurso.Solicitud.CiudadId)
            {
                return ResultadoOperacion.Falla(Error(
                    "VoluntarioOtraCiudad",
                    "El voluntario no pertenece a la ciudad de la solicitud."));
            }

            if (voluntario.ZonaId.HasValue
                && voluntario.ZonaId != recurso.Solicitud.ZonaId)
            {
                return ResultadoOperacion.Falla(Error(
                    "VoluntarioOtraZona",
                    "El voluntario eligió otra zona para voluntariado."));
            }

            if (!esAdministrador && actorId != voluntarioId.Value)
            {
                return ResultadoOperacion.Falla(Error(
                    "NoAutorizado",
                    "Un voluntario solo puede asignarse a sí mismo."));
            }
        }
        else
        {
            var organizacionDestinoId = organizacionId!.Value;

            var organizacion = await _db.Organizacion
                .AsNoTracking()
                .Where(o => o.Id == organizacionDestinoId)
                .Select(o => new { o.Estado, o.CiudadId })
                .SingleOrDefaultAsync(cancellationToken);

            if (organizacion is null)
            {
                return ResultadoOperacion.Falla(Error(
                    "OrganizacionNoEncontrada",
                    "La organización no existe."));
            }

            if (organizacion.Estado != "activa")
            {
                return ResultadoOperacion.Falla(Error(
                    "OrganizacionInactiva",
                    "La organización no está activa y no puede tomar solicitudes."));
            }

            if (organizacion.CiudadId != recurso.Solicitud.CiudadId)
            {
                return ResultadoOperacion.Falla(Error(
                    "OrganizacionOtraCiudad",
                    "La organización no pertenece a la ciudad de la solicitud."));
            }

            if (!esAdministrador)
            {
                var esMiembroActivo = await _db.OrganizacionUsuario
                    .AsNoTracking()
                    .AnyAsync(
                        ou => ou.OrganizacionId == organizacionDestinoId
                            && ou.UsuarioId == actorId
                            && ou.Activo,
                        cancellationToken);

                if (!esMiembroActivo)
                {
                    return ResultadoOperacion.Falla(Error(
                        "NoAutorizado",
                        "Solo un miembro activo de la organización puede asignarla."));
                }
            }
        }

        var ahora = DateTime.UtcNow;
        var estadoAnterior = recurso.Solicitud.Estado;

        await using var transaccion = await _db.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            _db.Asignacion.Add(new Asignacion
            {
                Id = Guid.NewGuid(),
                SolicitudRecursoId = solicitudRecursoId,
                VoluntarioId = voluntarioId,
                OrganizacionId = organizacionId,
                Estado = "asignada",
                AsignadaPor = actorId,
                AsignadaAt = ahora,
                CreadoAt = ahora,
                ActualizadoAt = ahora
            });

            await _db.SaveChangesAsync(cancellationToken);

            recurso.Estado = "asignada";
            recurso.ActualizadoAt = ahora;

            if (estadoAnterior == "pendiente")
            {
                recurso.Solicitud.Estado = "asignada";
                recurso.Solicitud.ActualizadoAt = ahora;

                _db.HistorialSolicitud.Add(new HistorialSolicitud
                {
                    Id = Guid.NewGuid(),
                    SolicitudId = recurso.SolicitudId,
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = "asignada",
                    ActorId = actorId,
                    Motivo = "Se asignó un recurso solicitado.",
                    CreadoAt = ahora
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Falla(Error(
                "ConflictoDeEstado",
                "La solicitud cambió mientras se asignaba. Actualizá los datos y volvé a intentar."));
        }

        return ResultadoOperacion.Ok();
    }

    /// <summary>
    /// Cancela una solicitud abierta. Solo puede hacerlo el solicitante o un
    /// administrador, y siempre se exige un motivo.
    ///
    /// Si había asignaciones activas, primero se cancelan (la base no permite
    /// cancelar un recurso que todavía tiene una asignación activa) y luego se
    /// cancelan los recursos no resueltos, la solicitud y se registra el
    /// historial. Todo ocurre dentro de una transacción.
    /// </summary>
    public async Task<ResultadoOperacion> CancelarAsync(
        Guid actorId,
        Guid solicitudId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var motivoLimpio = Limpiar(motivo);
        if (motivoLimpio is null)
        {
            return ResultadoOperacion.Falla(Error(
                "MotivoRequerido",
                "El motivo de cancelación es obligatorio."));
        }

        if (motivoLimpio.Length > LargoMaximoMotivoCancelacion)
        {
            return ResultadoOperacion.Falla(Error(
                "MotivoMuyLargo",
                $"El motivo no puede superar los {LargoMaximoMotivoCancelacion} caracteres."));
        }

        var actor = await _userManager.FindByIdAsync(actorId.ToString());
        if (actor is null || !actor.Activo)
        {
            return ResultadoOperacion.Falla(Error(
                "UsuarioInvalido",
                "El usuario no existe o está inactivo."));
        }

        var solicitud = await _db.Solicitud
            .Include(s => s.SolicitudRecurso)
                .ThenInclude(sr => sr.Asignaciones)
            .SingleOrDefaultAsync(s => s.Id == solicitudId, cancellationToken);

        if (solicitud is null)
        {
            return ResultadoOperacion.Falla(Error(
                "SolicitudNoEncontrada",
                "La solicitud no existe."));
        }

        if (EstadosSolicitudCerrada.Contains(solicitud.Estado))
        {
            return ResultadoOperacion.Falla(Error(
                "SolicitudCerrada",
                "La solicitud ya está resuelta o cancelada."));
        }

        var esAdministrador = await _userManager.IsInRoleAsync(
            actor,
            UsuarioAppService.RolAdministrador);

        if (!esAdministrador && solicitud.SolicitanteId != actorId)
        {
            return ResultadoOperacion.Falla(Error(
                "NoAutorizado",
                "Solo el solicitante o un administrador pueden cancelar la solicitud."));
        }

        var asignacionesActivas = solicitud.SolicitudRecurso
            .SelectMany(r => r.Asignaciones)
            .Where(a => EstadosAsignacionActiva.Contains(a.Estado))
            .ToList();

        var asignacionConfirmada = asignacionesActivas
            .FirstOrDefault(a => a.Estado == "completada" && a.ConfirmadaPor is not null);

        if (asignacionConfirmada is not null)
        {
            return ResultadoOperacion.Falla(Error(
                "AsignacionConfirmada",
                "No se puede cancelar la solicitud porque tiene una asignación completada y confirmada."));
        }

        var ahora = DateTime.UtcNow;
        var estadoAnterior = solicitud.Estado;

        await using var transaccion = await _db.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var asignacion in asignacionesActivas)
            {
                asignacion.Estado = "cancelada";
                asignacion.CanceladaPor = actorId;
                asignacion.CanceladaAt = ahora;
                asignacion.MotivoCancelacion = motivoLimpio;
                asignacion.ActualizadoAt = ahora;
            }

            if (asignacionesActivas.Count > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            foreach (var recurso in solicitud.SolicitudRecurso)
            {
                if (recurso.Estado is "resuelta" or "cancelada")
                {
                    continue;
                }

                recurso.Estado = "cancelada";
                recurso.ResueltaAt = null;
                recurso.ActualizadoAt = ahora;
            }

            solicitud.Estado = "cancelada";
            solicitud.MotivoCancelacion = motivoLimpio;
            solicitud.CanceladaPor = actorId;
            solicitud.CanceladaAt = ahora;
            solicitud.ActualizadoAt = ahora;

            _db.HistorialSolicitud.Add(new HistorialSolicitud
            {
                Id = Guid.NewGuid(),
                SolicitudId = solicitudId,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = "cancelada",
                ActorId = actorId,
                Motivo = motivoLimpio,
                CreadoAt = ahora
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Falla(Error(
                "ConflictoDeEstado",
                "La solicitud cambió mientras se cancelaba. Actualizá los datos y volvé a intentar."));
        }

        return ResultadoOperacion.Ok();
    }

    /// <summary>
    /// Convierte texto vacío o con espacios en null. Así la base no guarda
    /// cadenas vacías donde el esquema espera null.
    /// </summary>
    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ErrorNegocio Error(string codigo, string mensaje) =>
        new(codigo, mensaje);
}

using AyudaYa.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AyudaYa.Negocio.Servicios;

/// <summary>
/// Datos generales de la aplicación (sección "App"). <see cref="UrlBase"/> es la
/// dirección pública (por ejemplo https://ayudaya.onrender.com) que se usa para
/// armar los enlaces de los correos. Sin ella, los avisos de solicitudes salen
/// sin enlace, los correos de confirmación usan la dirección del pedido y la
/// recuperación de contraseña solo se envía en Development (fuera de desarrollo
/// exige <see cref="UrlBase"/> para evitar envenenamiento del encabezado Host).
/// </summary>
public sealed class AppOptions
{
    public const string Seccion = "App";

    public string? UrlBase { get; set; }

    /// <summary>La dirección pública sin barra final, o null si falta o no es http(s) absoluta.</summary>
    public string? UrlBaseNormalizada =>
        Uri.TryCreate(UrlBase?.Trim(), UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            ? uri.GetLeftPart(UriPartial.Path).TrimEnd('/')
            : null;
}

/// <summary>
/// Avisos por correo a quien pidió ayuda: cuando alguien toma un recurso de su
/// solicitud y cuando lo marca como completado. Es un envío de mejor esfuerzo:
/// un fallo del proveedor de correo se registra pero nunca revierte ni hace
/// fallar la operación que lo originó. Los avisos no incluyen teléfono,
/// dirección ni otros datos de contacto.
/// </summary>
public sealed class NotificacionesAppService
{
    private readonly AyudaYaDbContext _db;
    private readonly IEnviadorCorreo _correo;
    private readonly AppOptions _app;
    private readonly ILogger<NotificacionesAppService> _logger;

    public NotificacionesAppService(
        AyudaYaDbContext db,
        IEnviadorCorreo correo,
        IOptions<AppOptions> app,
        ILogger<NotificacionesAppService> logger)
    {
        _db = db;
        _correo = correo;
        _app = app.Value;
        _logger = logger;
    }

    /// <summary>Avisa que alguien tomó un recurso de la solicitud.</summary>
    public Task AvisarRecursoTomadoAsync(
        Guid asignacionId, Guid actorId, CancellationToken cancellationToken = default) =>
        AvisarAsync(asignacionId, actorId, completado: false, cancellationToken);

    /// <summary>Avisa que quien atendía marcó el recurso como completado y falta confirmarlo.</summary>
    public Task AvisarRecursoCompletadoAsync(
        Guid asignacionId, Guid actorId, CancellationToken cancellationToken = default) =>
        AvisarAsync(asignacionId, actorId, completado: true, cancellationToken);

    private async Task AvisarAsync(
        Guid asignacionId, Guid actorId, bool completado, CancellationToken cancellationToken)
    {
        try
        {
            var datos = await _db.Asignacion.AsNoTracking()
                .Where(a => a.Id == asignacionId)
                .Select(a => new
                {
                    SolicitudId = a.SolicitudRecurso.SolicitudId,
                    SolicitanteId = a.SolicitudRecurso.Solicitud.SolicitanteId,
                    SolicitanteNombre = a.SolicitudRecurso.Solicitud.Solicitante.NombreCompleto,
                    SolicitanteEmail = a.SolicitudRecurso.Solicitud.Solicitante.Email,
                    SolicitanteActivo = a.SolicitudRecurso.Solicitud.Solicitante.Activo,
                    Recurso = a.SolicitudRecurso.TipoRecurso.Nombre,
                    Voluntario = a.Voluntario == null ? null : a.Voluntario.Usuario.NombreCompleto,
                    Organizacion = a.Organizacion == null ? null : a.Organizacion.Nombre
                })
                .SingleOrDefaultAsync(cancellationToken);

            // Sin destinatario válido, o si la persona se atiende a sí misma, no hay nada que avisar.
            if (datos is null
                || !datos.SolicitanteActivo
                || string.IsNullOrWhiteSpace(datos.SolicitanteEmail)
                || datos.SolicitanteId == actorId)
            {
                return;
            }

            var codigo = ReglasNegocio.CodigoCorto(datos.SolicitudId);
            var quien = datos.Organizacion is not null
                ? $"La organización {datos.Organizacion}"
                : $"{ReglasNegocio.NombreCorto(datos.Voluntario)} (voluntariado)";
            var enlace = _app.UrlBaseNormalizada is { } baseUrl
                ? $"{baseUrl}/Solicitudes/Detalle/{datos.SolicitudId}"
                : null;

            var lineas = new List<string> { $"Hola {datos.SolicitanteNombre}," };
            lineas.Add(string.Empty);

            string asunto;
            if (completado)
            {
                asunto = $"Completaron la atención de tu solicitud {codigo}";
                lineas.Add($"{quien} marcó como completada la atención de «{datos.Recurso}» en tu solicitud {codigo}.");
                lineas.Add("Entrá a AyudaYa y confirmá que lo recibiste: así damos por resuelto ese recurso.");
            }
            else
            {
                asunto = $"Tomaron tu solicitud {codigo}";
                lineas.Add($"{quien} tomó «{datos.Recurso}» de tu solicitud {codigo} y va a ocuparse de atenderlo.");
                lineas.Add("Te avisamos de nuevo cuando lo marque como completado.");
            }

            if (enlace is not null)
            {
                lineas.Add(string.Empty);
                lineas.Add($"Podés seguir tu solicitud acá: {enlace}");
            }

            lineas.Add(string.Empty);
            lineas.Add("AyudaYa");

            await _correo.EnviarAsync(
                datos.SolicitanteEmail, asunto, string.Join(Environment.NewLine, lineas), cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // El aviso es accesorio: la operación ya quedó confirmada.
            _logger.LogWarning(ex, "No se pudo enviar el aviso de la asignación {AsignacionId}.", asignacionId);
        }
    }
}

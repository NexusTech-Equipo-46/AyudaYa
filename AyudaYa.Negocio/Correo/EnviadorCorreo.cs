using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AyudaYa.Negocio;

/// <summary>Salida de correo de la aplicación (confirmación de email).</summary>
public interface IEnviadorCorreo
{
    Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpo,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuración de correo (sección "Correo"). Orden de elección:
/// <see cref="ApiKey"/> (API HTTP de Brevo, funciona en hostings que bloquean SMTP),
/// luego <see cref="Host"/> (SMTP) y, si no hay ninguno, el enviador de registro,
/// que solo escribe el mensaje en el log.
/// Las claves van en user-secrets o en variables de entorno, nunca en el repo.
/// </summary>
public sealed class CorreoOptions
{
    public const string Seccion = "Correo";

    /// <summary>Clave de la API de Brevo (empieza con "xkeysib-").</summary>
    public string? ApiKey { get; set; }

    /// <summary>Nombre que ve quien recibe el correo (solo para la API).</summary>
    public string NombreRemitente { get; set; } = "AyudaYa";

    public string? Host { get; set; }

    public int Puerto { get; set; } = 587;

    public string? Usuario { get; set; }

    public string? Clave { get; set; }

    public string? Remitente { get; set; }

    public bool UsarSsl { get; set; } = true;
}

/// <summary>Envía por SMTP con STARTTLS/SSL.</summary>
public sealed class EnviadorCorreoSmtp : IEnviadorCorreo
{
    private readonly CorreoOptions _opciones;

    public EnviadorCorreoSmtp(IOptions<CorreoOptions> opciones)
    {
        _opciones = opciones.Value;
    }

    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpo,
        CancellationToken cancellationToken = default)
    {
        var remitente = string.IsNullOrWhiteSpace(_opciones.Remitente)
            ? _opciones.Usuario
            : _opciones.Remitente;

        using var mensaje = new MailMessage(remitente!, destinatario, asunto, cuerpo);
        using var cliente = new SmtpClient(_opciones.Host!, _opciones.Puerto)
        {
            EnableSsl = _opciones.UsarSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
        {
            cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Clave);
        }

        await cliente.SendMailAsync(mensaje, cancellationToken);
    }
}

/// <summary>
/// Envía por la API HTTPS de Brevo (plan gratuito: 300 correos por día). Se usa HTTPS
/// (puerto 443) porque los planes gratuitos de varios hostings bloquean los puertos SMTP.
/// El <see cref="CorreoOptions.Remitente"/> debe ser un remitente verificado en Brevo.
/// </summary>
public sealed class EnviadorCorreoBrevo : IEnviadorCorreo
{
    private static readonly Uri Destino = new("https://api.brevo.com/v3/smtp/email");

    private readonly CorreoOptions _opciones;
    private readonly HttpClient _http;

    public EnviadorCorreoBrevo(IOptions<CorreoOptions> opciones)
        : this(opciones, new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
    {
    }

    public EnviadorCorreoBrevo(IOptions<CorreoOptions> opciones, HttpClient http)
    {
        _opciones = opciones.Value;
        _http = http;
    }

    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_opciones.Remitente))
        {
            throw new InvalidOperationException("Correo:Remitente es obligatorio para enviar con Brevo.");
        }

        using var pedido = new HttpRequestMessage(HttpMethod.Post, Destino)
        {
            Content = JsonContent.Create(new
            {
                sender = new { name = _opciones.NombreRemitente, email = _opciones.Remitente },
                to = new[] { new { email = destinatario } },
                subject = asunto,
                textContent = cuerpo
            })
        };
        pedido.Headers.Add("api-key", _opciones.ApiKey);
        pedido.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        using var respuesta = await _http.SendAsync(pedido, cancellationToken);
        if (!respuesta.IsSuccessStatusCode)
        {
            // Solo el código: el cuerpo puede incluir datos del destinatario y nunca la clave.
            throw new InvalidOperationException($"Brevo rechazó el envío (HTTP {(int)respuesta.StatusCode}).");
        }
    }
}

/// <summary>
/// Sin SMTP configurado: escribe el correo en el log (útil en desarrollo,
/// donde el enlace de confirmación se copia desde la consola).
/// </summary>
public sealed class EnviadorCorreoRegistro : IEnviadorCorreo
{
    private readonly ILogger<EnviadorCorreoRegistro> _logger;

    public EnviadorCorreoRegistro(ILogger<EnviadorCorreoRegistro> logger)
    {
        _logger = logger;
    }

    public Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpo,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "SMTP no configurado. Correo para {Destinatario} — {Asunto}{NuevaLinea}{Cuerpo}",
            destinatario,
            asunto,
            Environment.NewLine,
            cuerpo);
        return Task.CompletedTask;
    }
}

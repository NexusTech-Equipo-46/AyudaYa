using System.Net;
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
/// Configuración SMTP (sección "Correo"). Si <see cref="Host"/> está vacío se
/// usa el enviador de registro, que solo escribe el mensaje en el log.
/// La clave va en user-secrets o en variables de entorno, nunca en el repo.
/// </summary>
public sealed class CorreoOptions
{
    public const string Seccion = "Correo";

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

namespace AyudaYa.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int Codigo { get; set; } = 500;

    public string Titulo => Codigo switch
    {
        404 => "No encontramos esa página",
        403 => "No tenés permiso para entrar acá",
        429 => "Demasiados intentos",
        _ => "Algo salió mal"
    };

    public string Mensaje => Codigo switch
    {
        404 => "Puede que el enlace esté incompleto o que lo que buscás ya no esté disponible.",
        403 => "Tu cuenta no tiene acceso a esta sección.",
        429 => "Esperá un minuto y volvé a intentar.",
        _ => "Tuvimos un problema al procesar tu pedido. Probá de nuevo en unos minutos."
    };
}

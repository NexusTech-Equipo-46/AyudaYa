namespace AyudaYa.Web.Models.Cuenta;

/// <param name="ReturnUrl">A dónde seguir después de confirmar (ya validado como dirección de la app).</param>
public sealed record ConfirmarEmailViewModel(bool Exito, string Mensaje, string? ReturnUrl = null)
{
    /// <summary>Texto del botón para seguir donde estaba la persona.</summary>
    public string TextoContinuar => (ReturnUrl?.Split('?')[0].TrimEnd('/').ToLowerInvariant()) switch
    {
        "/organizacion/registrar" => "Continuar a registrar la organización",
        "/solicitudes/nueva" => "Continuar a pedir ayuda",
        "/voluntariado" or "/voluntariado/index" => "Continuar a sumarte como voluntario",
        _ => "Continuar donde estabas"
    };
}

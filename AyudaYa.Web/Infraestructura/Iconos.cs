using Microsoft.AspNetCore.Html;

namespace AyudaYa.Web.Infraestructura;

/// <summary>
/// Íconos SVG en línea. Cada estado tiene forma propia: nunca se distingue
/// solo por color (accesibilidad).
/// </summary>
public static class Iconos
{
    private const string Trazo = "fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"";

    private static HtmlString Chico(string trazos) =>
        new($"<svg viewBox=\"0 0 16 16\" aria-hidden=\"true\" focusable=\"false\">{trazos}</svg>");

    private static HtmlString Grande(string trazos) =>
        new($"<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\" focusable=\"false\">{trazos}</svg>");

    public static readonly HtmlString Check = Chico(
        "<path d=\"M3 8.5l3.2 3L13 4.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");

    public static readonly HtmlString Cruz = Chico(
        "<path d=\"M4 4l8 8M12 4l-8 8\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\"/>");

    public static readonly HtmlString Guion = Chico(
        "<path d=\"M4 8h8\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"/>");

    public static readonly HtmlString Exclamacion = Chico(
        "<path d=\"M8 1.5L15 14H1z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linejoin=\"round\"/><path d=\"M8 6v3.5\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\"/><circle cx=\"8\" cy=\"11.8\" r=\"1\" fill=\"currentColor\"/>");

    public static readonly HtmlString Pausa = Chico(
        $"<circle cx=\"8\" cy=\"8\" r=\"6.5\" {Trazo}/><path d=\"M6.3 5.5v5M9.7 5.5v5\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\"/>");

    public static readonly HtmlString Reloj = Chico(
        $"<circle cx=\"8\" cy=\"8\" r=\"6.5\" {Trazo}/><path d=\"M8 4.5V8l2.5 1.5\" {Trazo} stroke-linecap=\"round\"/>");

    public static readonly HtmlString Persona = Chico(
        $"<circle cx=\"8\" cy=\"5\" r=\"2.6\" {Trazo}/><path d=\"M2.8 14c.4-3 2.5-4.6 5.2-4.6s4.8 1.6 5.2 4.6\" {Trazo} stroke-linecap=\"round\"/>");

    public static readonly HtmlString Corazon = Chico(
        $"<path d=\"M8 13.5S2 10 2 6a3 3 0 0 1 6-1 3 3 0 0 1 6 1c0 4-6 7.5-6 7.5z\" {Trazo} stroke-linejoin=\"round\"/>");

    public static readonly HtmlString Mitad = Chico(
        $"<circle cx=\"8\" cy=\"8\" r=\"6.5\" {Trazo}/><path d=\"M8 1.5a6.5 6.5 0 0 1 0 13z\" fill=\"currentColor\"/>");

    public static readonly HtmlString Flecha = Chico(
        $"<path d=\"M3 8h9M8.5 4l4 4-4 4\" {Trazo} stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");

    public static readonly HtmlString Candado = Chico(
        $"<rect x=\"3\" y=\"7\" width=\"10\" height=\"7\" rx=\"1.5\" {Trazo}/><path d=\"M5.2 7V5a2.8 2.8 0 0 1 5.6 0v2\" {Trazo}/>");

    public static readonly HtmlString Asignada = Chico(
        $"<rect x=\"3.5\" y=\"2.5\" width=\"9\" height=\"11.5\" rx=\"1.5\" {Trazo}/><path d=\"M6 2.5h4v2H6z\" {Trazo} stroke-linejoin=\"round\"/><path d=\"M5.8 8h4.4M5.8 10.5h3\" {Trazo} stroke-linecap=\"round\"/>");

    public static readonly HtmlString EnAtencion = Chico(
        $"<circle cx=\"8\" cy=\"8\" r=\"6.5\" {Trazo}/><path d=\"M6.8 5.6v4.8L10.6 8z\" fill=\"currentColor\"/>");

    public static readonly HtmlString Completada = Chico(
        $"<circle cx=\"8\" cy=\"8\" r=\"6.5\" {Trazo}/><path d=\"M5 8.3l2.2 2.2L11 6\" {Trazo} stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");

    public static readonly HtmlString Confirmada = Chico(
        "<circle cx=\"8\" cy=\"8\" r=\"7\" fill=\"currentColor\"/><path d=\"M5 8.3l2.2 2.2L11 6\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");

    public static readonly HtmlString Liberada = Chico(
        $"<path d=\"M6 4L3 7l3 3\" {Trazo} stroke-linecap=\"round\" stroke-linejoin=\"round\"/><path d=\"M3 7h6.5a3.5 3.5 0 0 1 0 7H7\" {Trazo} stroke-linecap=\"round\"/>");

    public static readonly HtmlString Barras3 = Chico(
        "<rect x=\"1\" y=\"9\" width=\"4\" height=\"6\" rx=\"1\" fill=\"currentColor\"/><rect x=\"6\" y=\"5\" width=\"4\" height=\"10\" rx=\"1\" fill=\"currentColor\"/><rect x=\"11\" y=\"1\" width=\"4\" height=\"14\" rx=\"1\" fill=\"currentColor\"/>");

    public static readonly HtmlString Barras2 = Chico(
        "<rect x=\"1\" y=\"9\" width=\"4\" height=\"6\" rx=\"1\" fill=\"currentColor\"/><rect x=\"6\" y=\"5\" width=\"4\" height=\"10\" rx=\"1\" fill=\"currentColor\"/><rect x=\"11.5\" y=\"1.5\" width=\"3\" height=\"13\" rx=\"1\" fill=\"none\" stroke=\"currentColor\"/>");

    public static readonly HtmlString Barras1 = Chico(
        "<rect x=\"1\" y=\"9\" width=\"4\" height=\"6\" rx=\"1\" fill=\"currentColor\"/><rect x=\"6.5\" y=\"5.5\" width=\"3\" height=\"9\" rx=\"1\" fill=\"none\" stroke=\"currentColor\"/><rect x=\"11.5\" y=\"1.5\" width=\"3\" height=\"13\" rx=\"1\" fill=\"none\" stroke=\"currentColor\"/>");

    // Íconos grandes para las alertas.
    public static readonly HtmlString AlertaPeligro = Grande(
        "<path d=\"M12 2.5L22.5 21h-21z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linejoin=\"round\"/><path d=\"M12 9v5\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\"/><circle cx=\"12\" cy=\"17.5\" r=\"1.3\" fill=\"currentColor\"/>");

    public static readonly HtmlString AlertaInfo = Grande(
        "<circle cx=\"12\" cy=\"12\" r=\"10\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"/><path d=\"M12 11v6\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\"/><circle cx=\"12\" cy=\"7.5\" r=\"1.3\" fill=\"currentColor\"/>");

    public static readonly HtmlString AlertaOk = Grande(
        "<circle cx=\"12\" cy=\"12\" r=\"10\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"/><path d=\"M7 12.5l3.3 3.2L17 9\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");

    public static HtmlString EstadoSolicitud(string estado) => estado switch
    {
        "pendiente" => Reloj,
        "asignada" => Persona,
        "en_atencion" => Corazon,
        "parcial" => Mitad,
        "resuelta" => Check,
        "cancelada" => Cruz,
        _ => Guion
    };

    public static HtmlString EstadoAsignacion(string estado) => estado switch
    {
        "asignada" => Asignada,
        "en_atencion" => EnAtencion,
        "completada" => Completada,
        "confirmada" => Confirmada,
        "cancelada" => Cruz,
        "liberada" => Liberada,
        _ => Guion
    };

    public static HtmlString Verificacion(string estado) => estado switch
    {
        "aprobado" or "activa" => Check,
        "pendiente" => Reloj,
        "rechazado" or "rechazada" => Cruz,
        "suspendido" or "suspendida" => Pausa,
        _ => Guion
    };

    public static HtmlString Nivel(string nivel) => nivel switch
    {
        "alta" => Barras3,
        "media" => Barras2,
        "baja" => Barras1,
        "sin_stock" => Cruz,
        _ => Guion
    };

    public static HtmlString EstadoOperativo(string? estado) => estado switch
    {
        "operativo" => Check,
        "saturado" => Exclamacion,
        "cerrado" => Pausa,
        _ => Guion
    };
}

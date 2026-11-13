using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

public sealed class SolicitudesController : Controller
{
    // Esta pantalla muestra exclusivamente los ejemplos del prototipo.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index(string? id) =>
        string.IsNullOrEmpty(id) ? View() : View("Detalle", id);

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Detalle(string? id) => View(model: id ?? "s-1004");

    // El formulario todavía es una demostración y no guarda solicitudes.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Nueva() => View();

    // Confirmación visual de la demostración; no recibe ni guarda pedidos reales.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Pendiente() => View();
}

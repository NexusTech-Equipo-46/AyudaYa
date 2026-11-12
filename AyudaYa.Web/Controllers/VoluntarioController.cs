using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

public sealed class VoluntarioController : Controller
{
    // El panel muestra datos ficticios y acciones simuladas del prototipo.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Panel() => View();

    // La lista reproduce las asignaciones ficticias de la sesión de demostración.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult MisAsignaciones() => View();

    // La vista reproduce el prototipo con asignaciones ficticias.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Asignacion(string? id) => View(model: id ?? "a-2001");
}

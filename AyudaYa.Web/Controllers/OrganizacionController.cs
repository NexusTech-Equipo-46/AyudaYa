using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

public sealed class OrganizacionController : Controller
{
    // El panel muestra exclusivamente información ficticia del prototipo.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Panel() => View();

    // La administración de miembros es una demostración en el navegador.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Miembros() => View();

    // Los centros y las altas de esta pantalla son ficticios.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Centros() => View();

    // La edición de recursos usa solamente los datos ficticios del prototipo.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Recursos(string? id) => View(model: id ?? "a1f3");

    // La lista reproduce los ejemplos del prototipo.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Asignaciones() => View();

    // El detalle usa exclusivamente los datos ficticios del prototipo.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Asignacion(string? id) => View(model: id ?? "a-301");
}

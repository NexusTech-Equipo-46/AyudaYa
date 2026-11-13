using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

[AllowAnonymous] // Mantener solo si el equipo confirma el acceso público.
public sealed class CentrosController : Controller
{
    private readonly CentrosPublicosAppService _centros;

    public CentrosController(CentrosPublicosAppService centros)
    {
        _centros = centros;
    }

    public IActionResult Index()
    {
        return View("Ejemplos");
    }

    public IActionResult Ejemplo() => View();

    // La lista conectada a la base se conserva mientras /Centros muestra la demo.
    public async Task<IActionResult> Reales(CancellationToken cancellationToken)
    {
        var centros = await _centros.ObtenerTodosAsync(cancellationToken);
        return View("Index", centros);
    }

    public async Task<IActionResult> Detalle(
        Guid id, CancellationToken cancellationToken)
    {
        var centro = await _centros.ObtenerPorIdAsync(id, cancellationToken);
        if (centro is null) return NotFound();

        var tiposRecurso = await _centros.ObtenerTiposRecursoAsync(cancellationToken);
        return View(new DetalleCentroViewModel { Centro = centro, TiposRecurso = tiposRecurso });
    }
}

using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

[AllowAnonymous] // Mantener solo si el equipo confirma el acceso público.
public sealed class CentrosController : Controller
{
    private readonly CentrosPublicosAppService _centros;
    private readonly ILogger<CentrosController> _logger;

    public CentrosController(CentrosPublicosAppService centros, ILogger<CentrosController> logger)
    {
        _centros = centros;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var centros = await _centros.ObtenerTodosAsync(cancellationToken);
            var tiposRecurso = await _centros.ObtenerTiposRecursoAsync(cancellationToken);
            return View(new MapaCentrosViewModel { Centros = centros, TiposRecurso = tiposRecurso });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudieron cargar los centros públicos.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(new MapaCentrosViewModel { ErrorCarga = true });
        }
    }

    public IActionResult Ejemplo() => View();

    // Conserva los enlaces anteriores mientras la ruta principal muestra datos reales.
    public IActionResult Reales() => LocalRedirect(Url.Action(nameof(Index))! + Request.QueryString);

    public async Task<IActionResult> Detalle(
        Guid id, CancellationToken cancellationToken)
    {
        var centro = await _centros.ObtenerPorIdAsync(id, cancellationToken);
        if (centro is null) return NotFound();

        var tiposRecurso = await _centros.ObtenerTiposRecursoAsync(cancellationToken);
        return View(new DetalleCentroViewModel { Centro = centro, TiposRecurso = tiposRecurso });
    }
}

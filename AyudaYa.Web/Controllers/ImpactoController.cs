using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

/// <summary>Métricas públicas: personas alcanzadas por recurso resuelto.</summary>
[AllowAnonymous]
public sealed class ImpactoController : Controller
{
    private readonly MetricasAppService _metricas;
    private readonly CatalogosAppService _catalogos;

    public ImpactoController(MetricasAppService metricas, CatalogosAppService catalogos)
    {
        _metricas = metricas;
        _catalogos = catalogos;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? ciudadId, CancellationToken cancellationToken)
    {
        var ciudades = await _catalogos.CiudadesActivasAsync(cancellationToken);
        var seleccionada = ciudades.Any(c => c.Id == ciudadId) ? ciudadId : null;

        return View(new ImpactoViewModel
        {
            Impacto = await _metricas.ObtenerImpactoAsync(seleccionada, 12, cancellationToken),
            Ciudades = ciudades,
            CiudadId = seleccionada
        });
    }
}

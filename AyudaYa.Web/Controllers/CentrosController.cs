using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

/// <summary>Mapa y detalle público de centros. No requiere cuenta.</summary>
[AllowAnonymous]
public sealed class CentrosController : Controller
{
    private readonly CentrosPublicosAppService _centros;
    private readonly CatalogosAppService _catalogos;
    private readonly MetricasAppService _metricas;

    public CentrosController(
        CentrosPublicosAppService centros,
        CatalogosAppService catalogos,
        MetricasAppService metricas)
    {
        _centros = centros;
        _catalogos = catalogos;
        _metricas = metricas;
    }

    public async Task<IActionResult> Index(string? recurso, CancellationToken cancellationToken)
    {
        var centros = await _centros.ObtenerTodosAsync(cancellationToken);
        var zonas = await _catalogos.ZonasActivasAsync(null, cancellationToken);
        var tiposCentro = await _catalogos.TiposCentroActivosAsync(cancellationToken);
        var tiposRecurso = await _catalogos.TiposRecursoActivosAsync(cancellationToken);
        var necesidades = await _metricas.ObtenerNecesidadesPorZonaAsync(null, cancellationToken);

        return View(new MapaCentrosViewModel
        {
            Centros = centros,
            Zonas = zonas,
            TiposCentro = tiposCentro,
            TiposRecurso = tiposRecurso,
            Necesidades = necesidades,
            RecursoInicial = tiposRecurso.Any(t => t.Codigo == recurso) ? recurso : null
        });
    }

    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancellationToken)
    {
        var centro = await _centros.ObtenerPorIdAsync(id, cancellationToken);
        if (centro is null)
        {
            return NotFound();
        }

        var tiposRecurso = await _centros.ObtenerTiposRecursoAsync(cancellationToken);
        return View(new DetalleCentroViewModel { Centro = centro, TiposRecurso = tiposRecurso });
    }
}

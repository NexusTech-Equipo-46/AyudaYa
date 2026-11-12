using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AyudaYa.Web.Models;
using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Web.Controllers;

public class HomeController : Controller
{
    private readonly CentrosPublicosAppService _centros;
    private readonly ILogger<HomeController> _logger;

    public HomeController(CentrosPublicosAppService centros, ILogger<HomeController> logger)
    {
        _centros = centros;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var centros = await _centros.ObtenerTodosAsync(cancellationToken);
            return View(new InicioViewModel
            {
                CentrosRecientes = centros
                    .Where(c => c.EstadoOperativo != "cerrado" && c.RecursosActualizadoAt.HasValue)
                    .OrderByDescending(c => c.RecursosActualizadoAt)
                    .Take(3)
                    .ToList()
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudieron cargar los centros recientes en el inicio.");
            return View(new InicioViewModel { CentrosNoDisponibles = true });
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

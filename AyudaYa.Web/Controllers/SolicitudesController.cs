using System.Globalization;
using System.Security.Claims;
using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Models.Solicitudes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

public sealed class SolicitudesController(SolicitudAppService solicitudes) : Controller
{
    // Estas acciones siguen siendo las demostraciones existentes.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index(string? id) =>
        string.IsNullOrEmpty(id) ? View() : View("Detalle", id);

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Detalle(string? id) => View(model: id ?? "s-1004");

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Pendiente() => View();

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Nueva(CancellationToken cancellationToken)
    {
        var opciones = await solicitudes.ObtenerOpcionesNuevaAsync(cancellationToken);
        var modelo = new NuevaSolicitudViewModel
        {
            Zonas = opciones.Zonas,
            NombresRecursos = opciones.TiposRecurso.ToDictionary(r => r.Id, r => r.Nombre),
            Recursos = opciones.TiposRecurso
                .Select(r => new RecursoNuevaSolicitud { TipoRecursoId = r.Id })
                .ToList()
        };
        return View(modelo);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nueva(
        NuevaSolicitudViewModel modelo,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(usuarioId, out var solicitanteId))
        {
            return Challenge();
        }

        var opciones = await solicitudes.ObtenerOpcionesNuevaAsync(cancellationToken);
        modelo.Zonas = opciones.Zonas;
        modelo.NombresRecursos = opciones.TiposRecurso.ToDictionary(r => r.Id, r => r.Nombre);

        var zona = opciones.Zonas.FirstOrDefault(z => z.Id == modelo.ZonaId);
        if (zona is null)
        {
            ModelState.AddModelError(nameof(modelo.ZonaId), "Elegí una zona activa.");
        }

        if (!LeerCoordenada(modelo.Latitud, out var latitud))
        {
            ModelState.AddModelError(nameof(modelo.Latitud), "Ingresá una latitud válida.");
        }
        if (!LeerCoordenada(modelo.Longitud, out var longitud))
        {
            ModelState.AddModelError(nameof(modelo.Longitud), "Ingresá una longitud válida.");
        }

        if (!ModelState.IsValid || zona is null)
        {
            return View(modelo);
        }

        var recursos = modelo.Recursos
            .Where(r => r.Seleccionado)
            .Select(r => new RecursoSolicitadoRequest(
                r.TipoRecursoId, r.Personas ?? 0, r.Detalle))
            .ToList();

        var solicitud = new CrearSolicitudRequest(
            zona.CiudadId, zona.Id, modelo.Urgencia, modelo.PersonasAfectadas,
            modelo.Descripcion, modelo.Direccion, modelo.Telefono,
            latitud, longitud, recursos);

        var resultado = await solicitudes.CrearAsync(
            solicitanteId, solicitud, cancellationToken);

        if (resultado.SolicitudId is Guid id)
        {
            TempData["SolicitudCreadaId"] = id.ToString();
            return RedirectToAction(nameof(Confirmacion));
        }

        foreach (var error in resultado.Errores)
        {
            ModelState.AddModelError(string.Empty, error.Mensaje);
        }
        return View(modelo);
    }

    [Authorize]
    [HttpGet]
    public IActionResult Confirmacion()
    {
        if (TempData.Peek("SolicitudCreadaId") is not string id ||
            !Guid.TryParse(id, out _))
        {
            return RedirectToAction(nameof(Nueva));
        }
        return View(model: id);
    }

    private static bool LeerCoordenada(string? texto, out double valor) =>
        double.TryParse(texto?.Trim().Replace(',', '.'), NumberStyles.Float,
            CultureInfo.InvariantCulture, out valor);
}
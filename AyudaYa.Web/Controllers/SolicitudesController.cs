using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Infraestructura;
using AyudaYa.Web.Models.Solicitudes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

/// <summary>Pedidos de ayuda de la persona autenticada.</summary>
[Authorize]
public sealed class SolicitudesController : ControladorAyudaYa
{
    private readonly SolicitudAppService _solicitudes;
    private readonly SolicitudConsultasAppService _consultas;
    private readonly AsignacionAppService _asignaciones;
    private readonly AdministracionAppService _admin;
    private readonly CatalogosAppService _catalogos;

    public SolicitudesController(
        SolicitudAppService solicitudes,
        SolicitudConsultasAppService consultas,
        AsignacionAppService asignaciones,
        AdministracionAppService admin,
        CatalogosAppService catalogos)
    {
        _solicitudes = solicitudes;
        _consultas = consultas;
        _asignaciones = asignaciones;
        _admin = admin;
        _catalogos = catalogos;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? estado, CancellationToken cancellationToken)
    {
        var todas = await _consultas.ListarMisSolicitudesAsync(UsuarioId, cancellationToken);
        var filtro = estado is "abiertas" or "cerradas" ? estado : "todas";
        var visibles = filtro switch
        {
            "abiertas" => todas.Where(s => s.EstaAbierta).ToList(),
            "cerradas" => todas.Where(s => !s.EstaAbierta).ToList(),
            _ => todas
        };

        ViewData["Filtro"] = filtro;
        ViewData["Total"] = todas.Count;
        return View(visibles);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _consultas.ObtenerDetalleAsync(UsuarioId, id, cancellationToken);
        return RespuestaDeError(resultado) ?? View(resultado.Dato);
    }

    [HttpGet]
    public async Task<IActionResult> Nueva(CancellationToken cancellationToken) =>
        View(await ArmarFormularioAsync(new NuevaSolicitudViewModel(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Nueva(NuevaSolicitudViewModel modelo, CancellationToken cancellationToken)
    {
        // Los nombres vienen del catálogo, no del formulario (solo se usan en los mensajes).
        var nombres = (await _catalogos.TiposRecursoActivosAsync(cancellationToken))
            .ToDictionary(t => t.Id, t => t.Nombre);
        foreach (var recurso in modelo.Recursos)
        {
            recurso.Nombre = nombres.GetValueOrDefault(recurso.TipoRecursoId, string.Empty);
        }

        var elegidos = modelo.Recursos.Select((r, indice) => (Recurso: r, Indice: indice))
            .Where(x => x.Recurso.Seleccionado)
            .ToList();

        // Solo se validan los recursos marcados; los demás no se envían.
        for (var i = 0; i < modelo.Recursos.Count; i++)
        {
            if (!modelo.Recursos[i].Seleccionado)
            {
                ModelState.Remove($"{nameof(modelo.Recursos)}[{i}].{nameof(RecursoSolicitudViewModel.Personas)}");
                ModelState.Remove($"{nameof(modelo.Recursos)}[{i}].{nameof(RecursoSolicitudViewModel.Detalle)}");
            }
        }

        if (elegidos.Count == 0)
        {
            ModelState.AddModelError("Recursos", "Elegí al menos un recurso que necesitás.");
        }

        foreach (var (recurso, indice) in elegidos)
        {
            if (recurso.Personas is null)
            {
                ModelState.AddModelError(
                    $"Recursos[{indice}].Personas",
                    $"Indicá cuántas personas necesitan {recurso.Nombre.ToLowerInvariant()}.");
            }
            else if (modelo.PersonasAfectadas is int total && recurso.Personas > total)
            {
                ModelState.AddModelError(
                    $"Recursos[{indice}].Personas",
                    $"Las personas que necesitan {recurso.Nombre.ToLowerInvariant()} no pueden ser más que las afectadas ({total}).");
            }
        }

        if (ModelState.IsValid)
        {
            var resultado = await _solicitudes.CrearAsync(
                UsuarioId,
                new CrearSolicitudRequest(
                    modelo.CiudadId!.Value,
                    modelo.ZonaId!.Value,
                    modelo.Urgencia,
                    modelo.PersonasAfectadas!.Value,
                    modelo.Descripcion,
                    modelo.Direccion ?? string.Empty,
                    modelo.Telefono ?? string.Empty,
                    modelo.Latitud,
                    modelo.Longitud,
                    elegidos.Select(x => new RecursoSolicitadoRequest(
                        x.Recurso.TipoRecursoId, x.Recurso.Personas!.Value, x.Recurso.Detalle)).ToList()),
                cancellationToken);

            if (resultado.Exito)
            {
                Exito("Tu solicitud quedó registrada. Mientras nadie la tome figura como pendiente; podés cancelarla cuando quieras.");
                return RedirectToAction(nameof(Detalle), new { id = resultado.SolicitudId });
            }

            AgregarErrores(resultado.Errores);
        }

        return View(await ArmarFormularioAsync(modelo, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Cancelar(Guid id, string? motivo, CancellationToken cancellationToken)
    {
        var detalle = await _consultas.ObtenerDetalleAsync(UsuarioId, id, cancellationToken);
        var denegado = RespuestaDeError(detalle);
        if (denegado is not null)
        {
            return denegado;
        }

        // Si cancela un administrador que no es el solicitante, queda en la auditoría.
        var resultado = detalle.Dato!.EsDelSolicitante
            ? await _solicitudes.CancelarAsync(UsuarioId, id, motivo ?? string.Empty, cancellationToken)
            : await _admin.CancelarSolicitudAsync(UsuarioId, id, motivo ?? string.Empty, cancellationToken);

        return Redirigir(resultado, "La solicitud se canceló.", nameof(Detalle), valores: new { id });
    }

    /// <summary>Quien pidió ayuda confirma que recibió un recurso: queda resuelto.</summary>
    [HttpPost]
    public async Task<IActionResult> Confirmar(Guid id, Guid asignacionId, CancellationToken cancellationToken)
    {
        var resultado = await _asignaciones.ConfirmarAsync(UsuarioId, asignacionId, cancellationToken);
        return Redirigir(resultado, "Gracias por confirmar. Marcamos ese recurso como resuelto.", nameof(Detalle), valores: new { id });
    }

    private async Task<NuevaSolicitudViewModel> ArmarFormularioAsync(
        NuevaSolicitudViewModel modelo, CancellationToken cancellationToken)
    {
        var ciudades = await _catalogos.CiudadesActivasAsync(cancellationToken);
        var zonas = await _catalogos.ZonasActivasAsync(null, cancellationToken);
        var tipos = await _catalogos.TiposRecursoActivosAsync(cancellationToken);

        modelo.Ciudades = ciudades;
        modelo.Zonas = zonas;

        // Con una sola ciudad no hace falta elegirla.
        if (modelo.CiudadId is null && ciudades.Count == 1)
        {
            modelo.CiudadId = ciudades[0].Id;
        }

        // Un bloque por tipo de recurso, conservando lo que la persona ya cargó.
        var anteriores = modelo.Recursos
            .GroupBy(r => r.TipoRecursoId)
            .ToDictionary(g => g.Key, g => g.First());
        modelo.Recursos = tipos.Select(t =>
        {
            var recurso = anteriores.TryGetValue(t.Id, out var previo) ? previo : new RecursoSolicitudViewModel();
            recurso.TipoRecursoId = t.Id;
            recurso.Nombre = t.Nombre;
            return recurso;
        }).ToList();

        return modelo;
    }
}

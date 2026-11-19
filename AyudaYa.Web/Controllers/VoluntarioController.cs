using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Infraestructura;
using AyudaYa.Web.Models.Voluntario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

/// <summary>Panel de la persona voluntaria: solicitudes para tomar y sus asignaciones.</summary>
[Authorize]
public sealed class VoluntarioController : ControladorAyudaYa
{
   private readonly VoluntarioAppService _voluntarios;
    private readonly SolicitudAppService _solicitudes;
    private readonly SolicitudConsultasAppService _consultas;
    private readonly AsignacionAppService _asignaciones;
    private readonly CatalogosAppService _catalogos;

    public VoluntarioController(
        VoluntarioAppService voluntarios,
        SolicitudAppService solicitudes,
        SolicitudConsultasAppService consultas,
        AsignacionAppService asignaciones,
        CatalogosAppService catalogos)
    {
        _voluntarios = voluntarios;
        _solicitudes = solicitudes;
        _consultas = consultas;
        _asignaciones = asignaciones;
        _catalogos = catalogos;
    }

    [HttpGet]
    public async Task<IActionResult> Panel(
        bool ampliar,
        string? tipoRecurso,
        string? urgencia,
        CancellationToken cancellationToken)
    {
        var voluntario = await _voluntarios.ObtenerAsync(UsuarioId, cancellationToken);
        if (voluntario is null)
        {
            return RedirectToAction("Index", "Voluntariado");
        }

        var mias = await _asignaciones.ListarDeVoluntarioAsync(UsuarioId, cancellationToken);
        var tipos = await _catalogos.TiposRecursoActivosAsync(cancellationToken);
        var disponibles = voluntario.Activo
            ? await _consultas.ListarDisponiblesAsync(
                UsuarioId,
                voluntario.CiudadId,
                ampliar ? null : voluntario.ZonaId,
                tipoRecurso,
                urgencia,
                cancellationToken)
            : [];

        return View(new PanelVoluntarioViewModel
        {
            Voluntario = voluntario,
            Activas = mias.Where(a => a.EsActiva).Take(3).ToList(),
            CantidadActivas = mias.Count(a => a.EsActiva),
            CantidadPorConfirmar = mias.Count(a => a.EsPorConfirmar),
            CantidadConfirmadas = mias.Count(a => a.Estado == "confirmada"),
            Disponibles = disponibles,
            Ampliar = ampliar || voluntario.ZonaId is null,
            TipoRecurso = tipoRecurso,
            Urgencia = urgencia,
            TiposRecurso = tipos
        });
    }

    [HttpGet]
    public async Task<IActionResult> MisAsignaciones(string? estado, CancellationToken cancellationToken)
    {
        var todas = await _asignaciones.ListarDeVoluntarioAsync(UsuarioId, cancellationToken);
        var filtro = estado is "activas" or "terminadas" or "sin_completar" ? estado : "todas";
        var visibles = filtro switch
        {
            "activas" => todas.Where(a => a.EsActiva).ToList(),
            "terminadas" => todas.Where(a => a.Estado is "completada" or "confirmada").ToList(),
            "sin_completar" => todas.Where(a => a.Estado is "cancelada" or "liberada").ToList(),
            _ => todas
        };

        return View(new ListaAsignacionesViewModel { Asignaciones = visibles, Filtro = filtro, Total = todas.Count });
    }

    [HttpGet]
    public async Task<IActionResult> Asignacion(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _asignaciones.ObtenerAsync(UsuarioId, id, cancellationToken);
        return RespuestaDeError(resultado) ?? View(resultado.Dato);
    }

    /// <summary>La persona toma un recurso pendiente para sí misma.</summary>
    [HttpPost]
    public async Task<IActionResult> Tomar(Guid recursoId, CancellationToken cancellationToken)
    {
        var resultado = await _solicitudes.AsignarAsync(UsuarioId, recursoId, UsuarioId, null, cancellationToken);
        return Redirigir(
            resultado,
            "Tomaste el recurso. Abrilo para ver los datos de contacto y empezar la atención.",
            resultado.Exito ? nameof(MisAsignaciones) : nameof(Panel));
    }

    [HttpPost]
    public async Task<IActionResult> Pausa(bool activo, CancellationToken cancellationToken)
    {
        var resultado = await _voluntarios.CambiarDisponibilidadAsync(UsuarioId, activo, cancellationToken);
        return Redirigir(
            resultado,
            activo ? "Volviste a estar disponible." : "Quedaste en pausa. Tus asignaciones vigentes siguen igual.",
            nameof(Panel));
    }

    [HttpPost]
    public async Task<IActionResult> Iniciar(Guid id, CancellationToken cancellationToken) =>
        Redirigir(await _asignaciones.IniciarAsync(UsuarioId, id, cancellationToken),
            "Empezaste la atención.", nameof(Asignacion), valores: new { id });

    [HttpPost]
    public async Task<IActionResult> Completar(Guid id, CancellationToken cancellationToken) =>
        Redirigir(await _asignaciones.CompletarAsync(UsuarioId, id, cancellationToken),
            "Marcaste la asignación como completada. Falta que la persona confirme que recibió la ayuda.",
            nameof(Asignacion), valores: new { id });

    [HttpPost]
    public async Task<IActionResult> Liberar(Guid id, string? motivo, CancellationToken cancellationToken)
    {
        var resultado = await _asignaciones.LiberarAsync(UsuarioId, id, motivo ?? string.Empty, cancellationToken);
        return Redirigir(resultado, "Liberaste la asignación: otra persona puede tomarla.",
            nameof(Asignacion), valores: new { id });
    }
}

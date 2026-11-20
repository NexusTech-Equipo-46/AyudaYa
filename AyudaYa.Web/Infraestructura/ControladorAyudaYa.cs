using System.Security.Claims;
using AyudaYa.Negocio.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Infraestructura;

/// <summary>
/// Base de los controladores: id del usuario, mensajes que sobreviven a la
/// redirección (TempData) y respuestas comunes para los resultados de Negocio.
/// </summary>
public abstract class ControladorAyudaYa : Controller
{
    public const string ClaveExito = "Exito";
    public const string ClaveError = "Error";

    protected Guid UsuarioId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    protected void Exito(string mensaje) => TempData[ClaveExito] = mensaje;

    protected void Error(string mensaje) => TempData[ClaveError] = mensaje;

    protected void AgregarErrores(IEnumerable<ErrorNegocio> errores, string campo = "")
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(campo, error.Mensaje);
        }
    }

    /// <summary>Traduce un resultado con error a 403, 404 o redirección con mensaje.</summary>
    protected IActionResult? RespuestaDeError<T>(Resultado<T> resultado)
    {
        if (resultado.Exito)
        {
            return null;
        }

        if (resultado.EsNoAutorizado || resultado.EsUsuarioInvalido)
        {
            return Forbid();
        }

        if (resultado.EsNoEncontrado)
        {
            return NotFound();
        }

        Error(resultado.Mensajes());
        return RedirectToAction("Index", "Panel");
    }

    /// <summary>Guarda el resultado en TempData y vuelve al destino indicado.</summary>
    protected IActionResult Redirigir(ResultadoOperacion resultado, string exito, string accion, string? controlador = null, object? valores = null)
    {
        if (resultado.Exito)
        {
            Exito(exito);
        }
        else
        {
            Error(resultado.Mensajes());
        }

        return controlador is null
            ? RedirectToAction(accion, valores)
            : RedirectToAction(accion, controlador, valores);
    }
}

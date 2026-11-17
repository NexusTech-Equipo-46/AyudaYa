using AyudaYa.Negocio.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Infraestructura;

/// <summary>
/// Menú principal. Muestra solo lo que corresponde a la persona: voluntariado
/// si es voluntaria, organización si pertenece a una y administración si
/// tiene el rol. Ocultar un enlace nunca reemplaza la autorización: cada
/// controlador y servicio vuelve a verificar.
/// </summary>
public sealed class NavegacionViewComponent : ViewComponent
{
    private readonly UsuarioAppService _usuarios;

    public NavegacionViewComponent(UsuarioAppService usuarios)
    {
        _usuarios = usuarios;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var contexto = await _usuarios.ObtenerContextoAsync(UserClaimsPrincipal, HttpContext.RequestAborted);
        return View(contexto);
    }
}

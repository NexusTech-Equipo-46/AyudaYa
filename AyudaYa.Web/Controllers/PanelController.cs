using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Infraestructura;
using AyudaYa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

/// <summary>Panel de inicio de la persona autenticada.</summary>
[Authorize]
public sealed class PanelController : ControladorAyudaYa
{
    private readonly UsuarioAppService _usuarios;
    private readonly SolicitudConsultasAppService _consultas;

    public PanelController(UsuarioAppService usuarios, SolicitudConsultasAppService consultas)
    {
        _usuarios = usuarios;
        _consultas = consultas;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var perfil = await _usuarios.ObtenerPerfilAsync(User);
        if (perfil is null)
        {
            return Challenge();
        }

        var contexto = await _usuarios.ObtenerContextoAsync(User, cancellationToken);
        var solicitudes = await _consultas.ListarMisSolicitudesAsync(UsuarioId, cancellationToken);

        return View(new PanelViewModel
        {
            Nombre = perfil.NombreCompleto,
            EmailConfirmado = perfil.EmailConfirmado,
            EsVoluntario = contexto?.EsVoluntario ?? false,
            Organizaciones = contexto?.Organizaciones ?? [],
            Abiertas = solicitudes.Where(s => s.EstaAbierta).Take(3).ToList(),
            CantidadAbiertas = solicitudes.Count(s => s.EstaAbierta),
            CantidadResueltas = solicitudes.Count(s => s.Estado == "resuelta")
        });
    }
}

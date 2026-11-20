using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Infraestructura;
using AyudaYa.Web.Models.Voluntario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

/// <summary>Alta y edición del perfil de voluntario ("Quiero ser voluntario").</summary>
[Authorize]
public sealed class VoluntariadoController : ControladorAyudaYa
{
    private readonly VoluntarioAppService _voluntarios;
    private readonly CatalogosAppService _catalogos;
    private readonly UsuarioAppService _usuarios;

    public VoluntariadoController(
        VoluntarioAppService voluntarios, CatalogosAppService catalogos, UsuarioAppService usuarios)
    {
        _voluntarios = voluntarios;
        _catalogos = catalogos;
        _usuarios = usuarios;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var actual = await _voluntarios.ObtenerAsync(UsuarioId, cancellationToken);
        var modelo = new VoluntariadoViewModel
        {
            YaEsVoluntario = actual is not null,
            CiudadId = actual?.CiudadId,
            ZonaId = actual?.ZonaId,
            Telefono = actual?.Telefono,
            Acepto = actual is not null
        };

        return View(await CompletarCatalogosAsync(modelo, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Index(VoluntariadoViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _voluntarios.InscribirAsync(
                UsuarioId,
                new InscribirVoluntarioRequest(
                    modelo.CiudadId!.Value, modelo.ZonaId, modelo.Telefono ?? string.Empty, modelo.Acepto),
                cancellationToken);

            if (resultado.Exito)
            {
                Exito(modelo.YaEsVoluntario
                    ? "Guardamos tus datos de voluntario."
                    : "¡Gracias por sumarte! Ya podés tomar solicitudes de tu zona.");
                return RedirectToAction("Panel", "Voluntario");
            }

            AgregarErrores(resultado.Errores);
        }

        return View(await CompletarCatalogosAsync(modelo, cancellationToken));
    }

    private async Task<VoluntariadoViewModel> CompletarCatalogosAsync(
        VoluntariadoViewModel modelo, CancellationToken cancellationToken)
    {
        modelo.Ciudades = await _catalogos.CiudadesActivasAsync(cancellationToken);
        modelo.Zonas = await _catalogos.ZonasActivasAsync(null, cancellationToken);
        if (modelo.CiudadId is null && modelo.Ciudades.Count == 1)
        {
            modelo.CiudadId = modelo.Ciudades[0].Id;
        }

        // Sin el correo confirmado la inscripción se rechaza: se avisa antes de mostrar el formulario.
        modelo.EmailConfirmado = (await _usuarios.ObtenerPerfilAsync(User))?.EmailConfirmado ?? false;
        return modelo;
    }
}

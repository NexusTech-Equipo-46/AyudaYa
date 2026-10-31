using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Models.Cuenta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

public sealed class CuentaController : Controller
{
    private readonly UsuarioAppService _usuarios;

    public CuentaController(UsuarioAppService usuarios)
    {
        _usuarios = usuarios;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Registro()
    {
        return View(new RegistroViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(
        RegistroViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _usuarios.RegistrarAsync(
            model.Email,
            model.Password,
            model.NombreCompleto,
            model.Dni,
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Si el alta salió bien, se intenta dejar la sesión iniciada.
        var login = await _usuarios.IniciarSesionAsync(
            model.Email,
            model.Password,
            recordarme: false);

        if (!login.Succeeded)
        {
            return RedirectToAction(nameof(Login));
        }

        TempData["Mensaje"] = "Cuenta creada. Ya podés solicitar ayuda.";
        return RedirectToAction(nameof(Perfil));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _usuarios.IniciarSesionAsync(
            model.Email,
            model.Password,
            model.Recordarme);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction(nameof(Perfil));
        }

        if (result.IsNotAllowed)
        {
            // Puede ser un usuario con Activo = false o email no confirmado.
            ModelState.AddModelError(
                string.Empty,
                "La cuenta no está habilitada para iniciar sesión.");
        }
        else if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "La cuenta está bloqueada temporariamente por intentos fallidos.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
        }

        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var perfil = await _usuarios.ObtenerPerfilAsync(User);
        if (perfil is null)
        {
            return Challenge();
        }

        return View(new PerfilViewModel
        {
            NombreCompleto = perfil.NombreCompleto,
            Email = perfil.Email,
            Roles = perfil.Roles,
            Activo = perfil.Activo
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _usuarios.CerrarSesionAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        return View();
    }
}

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

        TempData["Mensaje"] = "Cuenta creada. Para solicitar ayuda tenés que confirmar tu email.";
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
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Perfil()
    {
        var perfil = await _usuarios.ObtenerPerfilAsync(User);
        if (perfil is null)
        {
            return Challenge();
        }

        return View(CrearModeloPerfil(perfil));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> ActualizarPerfil(
        [Bind(Prefix = "Datos")] DatosPerfilViewModel datos, CancellationToken cancellationToken)
    {
        var perfil = await _usuarios.ObtenerPerfilAsync(User);
        if (perfil is null) return Challenge();
        if (ModelState.IsValid)
        {
            var resultado = await _usuarios.ActualizarPerfilAsync(User, datos.NombreCompleto, datos.Dni, cancellationToken);
            if (resultado.Succeeded)
            {
                TempData["PerfilExito"] = "Guardamos tus datos.";
                return RedirectToAction(nameof(Perfil));
            }
            foreach (var error in resultado.Errors)
            {
                var campo = error.Code switch { "DniInvalido" or "DniDuplicado" => "Datos.Dni", "NombreInvalido" => "Datos.NombreCompleto", _ => "Datos" };
                ModelState.AddModelError(campo, error.Description);
            }
        }
        var modelo = CrearModeloPerfil(perfil);
        modelo.Datos = datos;
        ViewData["FormularioConErrores"] = "datos";
        return View(nameof(Perfil), modelo);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> CambiarClave([Bind(Prefix = "Clave")] CambiarClaveViewModel clave)
    {
        var perfil = await _usuarios.ObtenerPerfilAsync(User);
        if (perfil is null) return Challenge();
        if (ModelState.IsValid)
        {
            var resultado = await _usuarios.CambiarPasswordAsync(User, clave.Actual, clave.Nueva);
            if (resultado.Succeeded)
            {
                TempData["ClaveExito"] = "Cambiaste tu contraseña.";
                return RedirectToAction(nameof(Perfil));
            }
            foreach (var error in resultado.Errors)
            {
                var campo = error.Code switch { "PasswordMismatch" => "Clave.Actual", "PasswordIgual" => "Clave.Nueva", var codigo when codigo.StartsWith("Password", StringComparison.Ordinal) => "Clave.Nueva", _ => "Clave" };
                var mensaje = error.Code == "PasswordMismatch" ? "La contraseña actual es incorrecta." : error.Description;
                ModelState.AddModelError(campo, mensaje);
            }
        }
        // No se vuelven a enviar las contraseñas a la vista después de un error.
        foreach (var campo in new[] { "Clave.Actual", "Clave.Nueva", "Clave.Confirmacion" })
            if (ModelState.ContainsKey(campo)) ModelState.SetModelValue(campo, rawValue: null, attemptedValue: null);
        ViewData["FormularioConErrores"] = "clave";
        return View(nameof(Perfil), CrearModeloPerfil(perfil));
    }

    private static PerfilViewModel CrearModeloPerfil(PerfilUsuarioDto perfil) => new()
    {
        Email = perfil.Email,
        Roles = perfil.Roles,
        Activo = perfil.Activo,
        EsVoluntario = perfil.EsVoluntario,
        Datos = new DatosPerfilViewModel { NombreCompleto = perfil.NombreCompleto, Dni = perfil.Dni }
    };

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

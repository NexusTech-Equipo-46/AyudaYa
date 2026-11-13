using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Infraestructura;
using AyudaYa.Web.Models.Cuenta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AyudaYa.Web.Controllers;

public sealed class CuentaController : ControladorAyudaYa
{
    private readonly UsuarioAppService _usuarios;
    private readonly IWebHostEnvironment _entorno;

    public CuentaController(UsuarioAppService usuarios, IWebHostEnvironment entorno)
    {
        _usuarios = usuarios;
        _entorno = entorno;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Panel");
        }

        return View(new RegistroViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("autenticacion")]
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

        // Correo de confirmación: sin confirmar no se puede pedir ayuda ni sumarse.
        var usuario = await _usuarios.ObtenerPorEmailAsync(model.Email, cancellationToken);
        if (usuario is not null)
        {
            await EnviarConfirmacionAsync(usuario.Id, cancellationToken);
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

        Exito("Cuenta creada. Te enviamos un correo para confirmar tu dirección: lo necesitás para pedir ayuda o sumarte como voluntario.");
        return RedirectToAction("Index", "Panel");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Panel");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("autenticacion")]
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

            return RedirectToAction("Index", "Panel");
        }

        if (result.IsNotAllowed)
        {
            // Usuario con Activo = false (baja lógica).
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

    /// <summary>Destino del enlace del correo. Confirma la dirección y muestra el resultado.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ConfirmarEmail(Guid userId, string? token)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
        {
            return View(new ConfirmarEmailViewModel(false, "El enlace no es válido."));
        }

        var resultado = await _usuarios.ConfirmarEmailAsync(userId, token);
        return View(resultado.Succeeded
            ? new ConfirmarEmailViewModel(true, "Listo, confirmamos tu correo. Ya podés pedir ayuda o sumarte como voluntario.")
            : new ConfirmarEmailViewModel(false, string.Join(" ", resultado.Errors.Select(e => e.Description))));
    }

    [Authorize]
    [HttpPost]
    [EnableRateLimiting("autenticacion")]
    public async Task<IActionResult> ReenviarConfirmacion(CancellationToken cancellationToken)
    {
        var resultado = await EnviarConfirmacionAsync(UsuarioId, cancellationToken);
        if (resultado.Exito)
        {
            Exito("Te enviamos un correo nuevo. Revisá tu bandeja de entrada y la carpeta de spam.");
        }
        else
        {
            Error(resultado.Mensajes());
        }

        return RedirectToAction(nameof(Perfil));
    }

    [Authorize]
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Perfil(CancellationToken cancellationToken)
    {
        var perfil = await _usuarios.ObtenerPerfilAsync(User);
        if (perfil is null)
        {
            return Challenge();
        }

        return View(await CrearModeloPerfilAsync(perfil, cancellationToken));
    }

    [Authorize]
    [HttpPost]
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
        var modelo = await CrearModeloPerfilAsync(perfil, cancellationToken);
        modelo.Datos = datos;
        ViewData["FormularioConErrores"] = "datos";
        return View(nameof(Perfil), modelo);
    }

    [Authorize]
    [HttpPost]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> CambiarClave(
        [Bind(Prefix = "Clave")] CambiarClaveViewModel clave, CancellationToken cancellationToken)
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
        return View(nameof(Perfil), await CrearModeloPerfilAsync(perfil, cancellationToken));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _usuarios.CerrarSesionAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private async Task<PerfilViewModel> CrearModeloPerfilAsync(PerfilUsuarioDto perfil, CancellationToken cancellationToken)
    {
        var contexto = await _usuarios.ObtenerContextoAsync(User, cancellationToken);
        return new PerfilViewModel
        {
            Email = perfil.Email,
            Roles = perfil.Roles,
            Activo = perfil.Activo,
            EsVoluntario = perfil.EsVoluntario,
            EmailConfirmado = perfil.EmailConfirmado,
            Organizaciones = contexto?.Organizaciones ?? [],
            Datos = new DatosPerfilViewModel { NombreCompleto = perfil.NombreCompleto, Dni = perfil.Dni }
        };
    }

    private async Task<Resultado<string>> EnviarConfirmacionAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var resultado = await _usuarios.EnviarConfirmacionAsync(
            usuarioId,
            (id, token) => Url.Action(nameof(ConfirmarEmail), "Cuenta", new { userId = id, token }, Request.Scheme)!,
            cancellationToken);

        // En desarrollo sin SMTP el enlace se muestra en pantalla para poder probar el flujo.
        if (resultado.Exito && _entorno.IsDevelopment())
        {
            TempData["EnlaceDesarrollo"] = resultado.Dato;
        }

        return resultado;
    }
}

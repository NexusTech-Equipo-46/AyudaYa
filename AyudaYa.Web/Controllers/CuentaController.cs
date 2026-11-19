using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Infraestructura;
using AyudaYa.Web.Models.Cuenta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AyudaYa.Web.Controllers;

public sealed class CuentaController : ControladorAyudaYa
{
    private readonly UsuarioAppService _usuarios;
    private readonly IWebHostEnvironment _entorno;
    private readonly AppOptions _app;
    private readonly ILogger<CuentaController> _logger;

    public CuentaController(
        UsuarioAppService usuarios,
        IWebHostEnvironment entorno,
        IOptions<AppOptions> app,
        ILogger<CuentaController> logger)
    {
        _usuarios = usuarios;
        _entorno = entorno;
        _app = app.Value;
        _logger = logger;
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
        var correoEnviado = true;
        if (usuario is not null)
        {
            correoEnviado = (await EnviarConfirmacionAsync(usuario.Id, cancellationToken)).Exito;
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

        if (correoEnviado)
        {
            Exito("Cuenta creada. Te enviamos un correo para confirmar tu dirección: lo necesitás para pedir ayuda o sumarte como voluntario.");
        }
        else
        {
            Error("Cuenta creada, pero no pudimos enviarte el correo de confirmación. Pedilo de nuevo desde tu perfil en unos minutos.");
        }

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

    /// <summary>Pantalla para pedir el enlace de recuperación de contraseña.</summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult OlvideClave()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Perfil));
        }

        return View(new OlvideClaveViewModel());
    }

    /// <summary>
    /// Siempre responde lo mismo, exista o no la cuenta, para no revelar qué
    /// correos están registrados.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("autenticacion")]
    public async Task<IActionResult> OlvideClave(OlvideClaveViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Sin la dirección pública configurada el enlace dependería del encabezado Host, que
        // quien pide la recuperación puede falsear para llevar el token a otro sitio. Fuera de
        // desarrollo, en ese caso no se envía nada (la respuesta sigue siendo la misma).
        if (_app.UrlBaseNormalizada is null && !_entorno.IsDevelopment())
        {
            _logger.LogError("Recuperar contraseña desactivado: falta configurar App:UrlBase (dirección pública del sitio).");
        }
        else
        {
            await _usuarios.SolicitarRestablecimientoAsync(
                model.Email,
                (id, token) => EnlaceAbsoluto(nameof(RestablecerClave), new { userId = id, token }),
                cancellationToken);
        }

        return View(nameof(OlvideClave), new OlvideClaveViewModel { Email = model.Email, Enviado = true });
    }

    /// <summary>Destino del enlace del correo: formulario para elegir la contraseña nueva.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult RestablecerClave(Guid userId, string? token)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
        {
            return View("RestablecerClaveInvalido");
        }

        return View(new RestablecerClaveViewModel { UserId = userId, Token = token });
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("autenticacion")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> RestablecerClave(RestablecerClaveViewModel model, CancellationToken cancellationToken)
    {
        if (model.UserId == Guid.Empty || string.IsNullOrWhiteSpace(model.Token))
        {
            return View("RestablecerClaveInvalido");
        }

        if (ModelState.IsValid)
        {
            var resultado = await _usuarios.RestablecerClaveAsync(
                model.UserId, model.Token, model.Nueva, cancellationToken);

            if (resultado.Succeeded)
            {
                Exito("Listo, cambiaste tu contraseña. Ya podés iniciar sesión con la nueva.");
                return RedirectToAction(nameof(Login));
            }

            if (resultado.Errors.Any(e => e.Code == "TokenInvalido"))
            {
                return View("RestablecerClaveInvalido");
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(nameof(model.Nueva), error.Code switch
                {
                    "PasswordTooShort" => "La contraseña debe tener al menos 8 caracteres.",
                    "PasswordRequiresUpper" => "La contraseña necesita al menos una letra mayúscula.",
                    "PasswordRequiresLower" => "La contraseña necesita al menos una letra minúscula.",
                    "PasswordRequiresDigit" => "La contraseña necesita al menos un número.",
                    _ => "La contraseña no cumple los requisitos."
                });
            }
        }

        // No se vuelven a enviar las contraseñas a la vista después de un error.
        foreach (var campo in new[] { nameof(model.Nueva), nameof(model.Confirmacion) })
        {
            if (ModelState.ContainsKey(campo)) ModelState.SetModelValue(campo, rawValue: null, attemptedValue: null);
        }

        model.Nueva = string.Empty;
        model.Confirmacion = string.Empty;
        return View(model);
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

    /// <summary>
    /// URL absoluta para los correos. Usa la dirección pública configurada
    /// (App:UrlBase) y, solo si falta, la del pedido: así un encabezado Host
    /// manipulado no puede llevar a quien recibe el correo a otro sitio.
    /// </summary>
    private string EnlaceAbsoluto(string accion, object valores)
    {
        var ruta = Url.Action(accion, "Cuenta", valores)!;
        return _app.UrlBaseNormalizada is { } baseUrl
            ? baseUrl + ruta
            : $"{Request.Scheme}://{Request.Host}{ruta}";
    }

    private async Task<Resultado<string>> EnviarConfirmacionAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var resultado = await _usuarios.EnviarConfirmacionAsync(
            usuarioId,
            (id, token) => EnlaceAbsoluto(nameof(ConfirmarEmail), new { userId = id, token }),
            cancellationToken);

        // En desarrollo sin SMTP el enlace se muestra en pantalla para poder probar el flujo.
        if (resultado.Exito && _entorno.IsDevelopment())
        {
            TempData["EnlaceDesarrollo"] = resultado.Dato;
        }

        return resultado;
    }
}

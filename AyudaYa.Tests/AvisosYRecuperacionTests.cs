using AyudaYa.Data;
using AyudaYa.Negocio;
using AyudaYa.Negocio.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace AyudaYa.Tests;

/// <summary>Guarda los correos en memoria; opcionalmente falla como un proveedor caído.</summary>
public sealed class CorreoEnMemoria : IEnviadorCorreo
{
    public List<(string Para, string Asunto, string Cuerpo)> Enviados { get; } = [];

    public bool Falla { get; set; }

    /// <summary>Simula el vencimiento del HttpClient (TaskCanceledException) en lugar de un error común.</summary>
    public bool Vence { get; set; }

    public Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken = default)
    {
        if (Vence)
        {
            throw new TaskCanceledException("Tiempo de espera agotado.");
        }

        if (Falla)
        {
            throw new InvalidOperationException("Proveedor de correo caído.");
        }

        Enviados.Add((destinatario, asunto, cuerpo));
        return Task.CompletedTask;
    }
}

public class AppOptionsTests
{
    [Theory]
    [InlineData("https://ayudaya.onrender.com", "https://ayudaya.onrender.com")]
    [InlineData("  https://ayudaya.onrender.com/  ", "https://ayudaya.onrender.com")]
    [InlineData("http://localhost:5000", "http://localhost:5000")]
    [InlineData("https://sitio.test/app/", "https://sitio.test/app")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("ayudaya.onrender.com", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("https://sitio.test/?x=1", null)]
    public void La_direccion_publica_solo_se_acepta_si_es_http_absoluta(string? valor, string? esperado)
    {
        Assert.Equal(esperado, new AppOptions { UrlBase = valor }.UrlBaseNormalizada);
    }
}

/// <summary>Avisos por correo al tomar o completar un recurso, y recuperar contraseña (con base de datos).</summary>
public class AvisosYRecuperacionTests
{
    private static string TokenDelEnlace(string cuerpo, out Guid usuarioId)
    {
        var enlace = cuerpo.Split(Environment.NewLine).Single(l => l.StartsWith("https://ayudaya.test/Cuenta/RestablecerClave"));
        var consulta = QueryHelpers.ParseQuery(new Uri(enlace).Query);
        usuarioId = Guid.Parse(consulta["userId"]!);
        return consulta["token"]!;
    }

    private static async Task<string> EmailDeAsync(Escenario e, Guid usuarioId) =>
        (await e.Servicio<UserManager<UsuarioIdentity>>().FindByIdAsync(usuarioId.ToString()))!.Email!;

    [FactBaseDeDatos]
    public async Task Tomar_y_completar_avisan_a_quien_pidio_ayuda_sin_datos_de_contacto()
    {
        var correo = new CorreoEnMemoria();
        await using var e = new Escenario(correo);
        await e.PrepararAsync();

        var solicitante = await e.CrearUsuarioAsync("Marina");
        var voluntario = await e.CrearVoluntarioAsync("Lucas", e.ZonaId);
        var solicitud = await e.CrearSolicitudAsync(solicitante, null, ("abrigo", 3));
        var recurso = solicitud.Recursos.Single();
        var solicitudes = e.Servicio<SolicitudAppService>();
        var asignaciones = e.Servicio<AsignacionAppService>();
        var emailSolicitante = await EmailDeAsync(e, solicitante);

        Assert.True((await solicitudes.AsignarAsync(voluntario, recurso.Id, voluntario, null)).Exito);

        var aviso = Assert.Single(correo.Enviados);
        Assert.Equal(emailSolicitante, aviso.Para);
        Assert.Contains("Tomaron tu solicitud", aviso.Asunto);
        Assert.Contains("Lucas", aviso.Cuerpo);
        Assert.Contains($"https://ayudaya.test/Solicitudes/Detalle/{solicitud.Id}", aviso.Cuerpo);
        Assert.DoesNotContain("5555-0123", aviso.Cuerpo);
        Assert.DoesNotContain("Calle Falsa", aviso.Cuerpo);

        // Iniciar no genera aviso; completar sí.
        var asignacionId = (await asignaciones.ListarDeVoluntarioAsync(voluntario)).Single().Id;
        Assert.True((await asignaciones.IniciarAsync(voluntario, asignacionId)).Exito);
        Assert.Single(correo.Enviados);

        Assert.True((await asignaciones.CompletarAsync(voluntario, asignacionId)).Exito);
        Assert.Equal(2, correo.Enviados.Count);
        var completado = correo.Enviados[1];
        Assert.Equal(emailSolicitante, completado.Para);
        Assert.Contains("Completaron la atención", completado.Asunto);
        Assert.Contains("confirmá", completado.Cuerpo);
        Assert.DoesNotContain("5555-0123", completado.Cuerpo);
    }

    [FactBaseDeDatos]
    public async Task Un_fallo_del_correo_no_revierte_la_operacion()
    {
        await VerificarTomarConProveedorCaidoAsync(new CorreoEnMemoria { Falla = true });
    }

    [FactBaseDeDatos]
    public async Task Un_tiempo_de_espera_agotado_del_correo_tampoco_rompe_la_operacion()
    {
        await VerificarTomarConProveedorCaidoAsync(new CorreoEnMemoria { Vence = true });
    }

    private static async Task VerificarTomarConProveedorCaidoAsync(CorreoEnMemoria correo)
    {
        await using var e = new Escenario(correo);
        await e.PrepararAsync();

        var solicitante = await e.CrearUsuarioAsync("Marina");
        var voluntario = await e.CrearVoluntarioAsync("Lucas", e.ZonaId);
        var solicitud = await e.CrearSolicitudAsync(solicitante, null, ("agua", 2));

        var tomar = await e.Servicio<SolicitudAppService>().AsignarAsync(
            voluntario, solicitud.Recursos.Single().Id, voluntario, null);

        Assert.True(tomar.Exito, tomar.Mensajes());
        Assert.Empty(correo.Enviados);
        Assert.Equal("asignada", (await e.DetalleAsync(solicitante, solicitud.Id)).Estado);
    }

    [FactBaseDeDatos]
    public async Task Pedir_recuperacion_con_el_correo_caido_responde_igual_que_sin_cuenta()
    {
        var correo = new CorreoEnMemoria { Vence = true };
        await using var e = new Escenario(correo);
        await e.PrepararAsync();
        var usuarios = e.Servicio<UsuarioAppService>();
        var email = await EmailDeAsync(e, await e.CrearUsuarioAsync("Marina"));

        // Ni con cuenta existente ni sin ella hay excepción: no se distingue por el error.
        await usuarios.SolicitarRestablecimientoAsync(email, (u, t) => "https://ayudaya.test/x");
        await usuarios.SolicitarRestablecimientoAsync("nadie@prueba.test", (u, t) => "https://ayudaya.test/x");
    }

    [FactBaseDeDatos]
    public async Task Recuperar_contrasena_con_enlace_de_un_solo_uso()
    {
        var correo = new CorreoEnMemoria();
        await using var e = new Escenario(correo);
        await e.PrepararAsync();

        var usuarios = e.Servicio<UsuarioAppService>();
        var id = await e.CrearUsuarioAsync("Marina");
        var email = await EmailDeAsync(e, id);
        string Enlace(Guid usuario, string token) =>
            $"https://ayudaya.test/Cuenta/RestablecerClave?userId={usuario}&token={token}";

        // Un correo que no existe no genera mensaje ni error.
        await usuarios.SolicitarRestablecimientoAsync("nadie@prueba.test", Enlace);
        Assert.Empty(correo.Enviados);

        await usuarios.SolicitarRestablecimientoAsync(email, Enlace);
        var mensaje = Assert.Single(correo.Enviados);
        Assert.Equal(email, mensaje.Para);

        // Insistir dentro del minuto no manda otro correo.
        await usuarios.SolicitarRestablecimientoAsync(email, Enlace);
        Assert.Single(correo.Enviados);

        var token = TokenDelEnlace(mensaje.Cuerpo, out var usuarioId);
        Assert.Equal(id, usuarioId);

        // Una contraseña débil se rechaza sin gastar el enlace.
        var debil = await usuarios.RestablecerClaveAsync(id, token, "abc");
        Assert.False(debil.Succeeded);
        Assert.DoesNotContain(debil.Errors, x => x.Code == "TokenInvalido");

        // Un token alterado no sirve.
        var alterado = await usuarios.RestablecerClaveAsync(id, token + "x", "Nueva1234");
        Assert.Contains(alterado.Errors, x => x.Code == "TokenInvalido");

        var basura = await usuarios.RestablecerClaveAsync(id, "no-es-base64!", "Nueva1234");
        Assert.Contains(basura.Errors, x => x.Code == "TokenInvalido");

        var otroUsuario = await e.CrearUsuarioAsync("Otro");
        var ajeno = await usuarios.RestablecerClaveAsync(otroUsuario, token, "Nueva1234");
        Assert.Contains(ajeno.Errors, x => x.Code == "TokenInvalido");

        var bueno = await usuarios.RestablecerClaveAsync(id, token, "Nueva1234");
        Assert.True(bueno.Succeeded, string.Join("; ", bueno.Errors.Select(x => x.Description)));

        // La clave vieja ya no entra, la nueva sí, y se avisó del cambio.
        var manager = e.Servicio<UserManager<UsuarioIdentity>>();
        var usuario = (await manager.FindByIdAsync(id.ToString()))!;
        Assert.False(await manager.CheckPasswordAsync(usuario, Escenario.Password));
        Assert.True(await manager.CheckPasswordAsync(usuario, "Nueva1234"));
        Assert.Equal(2, correo.Enviados.Count);
        Assert.Contains("Cambiaste tu contraseña", correo.Enviados[1].Asunto);

        // El enlace no se puede reutilizar.
        var reuso = await usuarios.RestablecerClaveAsync(id, token, "Otra12345");
        Assert.Contains(reuso.Errors, x => x.Code == "TokenInvalido");
    }

    [FactBaseDeDatos]
    public async Task Una_cuenta_desactivada_no_puede_recuperar_la_contrasena()
    {
        var correo = new CorreoEnMemoria();
        await using var e = new Escenario(correo);
        await e.PrepararAsync();

        var usuarios = e.Servicio<UsuarioAppService>();
        var id = await e.CrearUsuarioAsync("Marina");
        var email = await EmailDeAsync(e, id);

        // El token se pide con la cuenta activa y se intenta usar después de la baja.
        await usuarios.SolicitarRestablecimientoAsync(email, (u, t) => $"https://ayudaya.test/Cuenta/RestablecerClave?userId={u}&token={t}");
        var token = TokenDelEnlace(Assert.Single(correo.Enviados).Cuerpo, out _);

        Assert.True((await usuarios.DesactivarAsync(id, "Prueba")).Succeeded);
        var resultado = await usuarios.RestablecerClaveAsync(id, token, "Nueva1234");

        Assert.Contains(resultado.Errors, x => x.Code == "TokenInvalido");
    }
}

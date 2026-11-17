using System.Net;
using System.Text.Json;
using AyudaYa.Negocio;
using Microsoft.Extensions.Options;

namespace AyudaYa.Tests;

/// <summary>El enviador por API: arma bien el pedido y no filtra la clave al fallar.</summary>
public class EnviadorCorreoBrevoTests
{
    private const string Clave = "xkeysib-clave-de-prueba";

    private sealed class ManejadorFalso(HttpStatusCode estado) : HttpMessageHandler
    {
        public HttpRequestMessage? Pedido { get; private set; }
        public string? Cuerpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Pedido = request;
            Cuerpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(estado) { Content = new StringContent("{\"message\":\"detalle interno\"}") };
        }
    }

    private static EnviadorCorreoBrevo Crear(ManejadorFalso manejador, string? remitente = "ayuda@ejemplo.test") =>
        new(
            Options.Create(new CorreoOptions { ApiKey = Clave, Remitente = remitente, NombreRemitente = "AyudaYa" }),
            new HttpClient(manejador));

    [Fact]
    public async Task Envia_el_pedido_a_la_api_con_clave_remitente_y_destinatario()
    {
        var manejador = new ManejadorFalso(HttpStatusCode.Created);

        await Crear(manejador).EnviarAsync("persona@ejemplo.test", "Asunto", "Cuerpo del mensaje");

        Assert.Equal("https://api.brevo.com/v3/smtp/email", manejador.Pedido!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, manejador.Pedido.Method);
        Assert.Equal(Clave, manejador.Pedido.Headers.GetValues("api-key").Single());

        using var json = JsonDocument.Parse(manejador.Cuerpo!);
        var raiz = json.RootElement;
        Assert.Equal("ayuda@ejemplo.test", raiz.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("persona@ejemplo.test", raiz.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Asunto", raiz.GetProperty("subject").GetString());
        Assert.Equal("Cuerpo del mensaje", raiz.GetProperty("textContent").GetString());
    }

    [Fact]
    public async Task Si_la_api_rechaza_falla_sin_mostrar_la_clave_ni_el_cuerpo()
    {
        var manejador = new ManejadorFalso(HttpStatusCode.Unauthorized);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Crear(manejador).EnviarAsync("persona@ejemplo.test", "Asunto", "Cuerpo"));

        Assert.Contains("401", error.Message);
        Assert.DoesNotContain(Clave, error.Message);
        Assert.DoesNotContain("detalle interno", error.Message);
    }

    [Fact]
    public async Task Sin_remitente_no_envia()
    {
        var manejador = new ManejadorFalso(HttpStatusCode.Created);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Crear(manejador, remitente: null).EnviarAsync("persona@ejemplo.test", "Asunto", "Cuerpo"));

        Assert.Null(manejador.Pedido);
    }
}

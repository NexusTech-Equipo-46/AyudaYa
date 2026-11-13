using System.Globalization;
using System.Threading.RateLimiting;
using AyudaYa.Negocio;
using AyudaYa.Negocio.Servicios;
using Microsoft.AspNetCore.Mvc;

// Cultura invariante para interpretar números y coordenadas: los campos
// type=number del navegador siempre envían el punto como separador decimal.
// Los textos y fechas se formatean explícitamente en es-AR (ver Formato).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// La composición de Data + Identity vive en Negocio. Web no referencia
// AyudaYa.Data ni consulta el DbContext.
builder.Services.AddAyudaYa(builder.Configuration);

// Todo POST exige token antifalsificación, sin tener que recordar el atributo.
builder.Services.AddControllersWithViews(opciones =>
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Freno a la fuerza bruta en login y registro (además del bloqueo de cuenta).
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.OnRejected = async (contexto, cancellationToken) =>
    {
        contexto.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await contexto.HttpContext.Response.WriteAsync(
            "Demasiados intentos. Esperá un minuto y volvé a probar.", cancellationToken);
    };
    opciones.AddPolicy("autenticacion", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?codigo={0}");

// Cabeceras de seguridad. No hay scripts en línea: todo el JavaScript es propio
// (se sirve desde /js y /lib), así que script-src puede ser solo 'self'.
app.Use(async (contexto, siguiente) =>
{
    var cabeceras = contexto.Response.Headers;
    cabeceras["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https://tile.openstreetmap.org; " +
        "connect-src 'self'; " +
        "font-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";
    cabeceras["X-Content-Type-Options"] = "nosniff";
    cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";
    cabeceras["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=()";
    cabeceras["X-Frame-Options"] = "DENY";
    await siguiente();
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await InicializarAsync(app);

app.Run();

// Primer administrador y, solo en desarrollo, datos de ejemplo.
static async Task InicializarAsync(WebApplication app)
{
    await using var alcance = app.Services.CreateAsyncScope();
    var logger = alcance.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Inicio");
    var configuracion = app.Configuration;

    try
    {
        var inicializador = alcance.ServiceProvider.GetRequiredService<InicializadorAppService>();
        var adminId = await inicializador.AsegurarAdministradorAsync(
            configuracion["Admin:Email"],
            configuracion["Admin:Password"],
            configuracion["Admin:Nombre"]);

        if (adminId is not null
            && app.Environment.IsDevelopment()
            && configuracion.GetValue<bool>("Seed:Demo"))
        {
            var passwordConfigurada = configuracion["Seed:PasswordDemo"];
            var passwordDemo = string.IsNullOrWhiteSpace(passwordConfigurada)
                ? InicializadorAppService.GenerarPasswordDemo()
                : passwordConfigurada;

            if (await inicializador.CargarDatosDemoAsync(adminId.Value, passwordDemo))
            {
                logger.LogWarning(
                    "Datos de ejemplo cargados. Usuarios: usuario@, voluntario@, camila@, responsable@, " +
                    "miembro@, defensa@ y pendiente@ejemplo.test.{Resto}",
                    string.IsNullOrWhiteSpace(passwordConfigurada)
                        ? $" Contraseña de todos (solo esta vez): {passwordDemo}"
                        : " Contraseña: la configurada en Seed:PasswordDemo.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo completar la inicialización (¿la base está disponible?).");
    }
}

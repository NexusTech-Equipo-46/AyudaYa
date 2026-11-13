using AyudaYa.Negocio;
using AyudaYa.Negocio.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AyudaYa.Tests;

/// <summary>
/// Las pruebas de integración corren contra una base PostgreSQL real (los
/// triggers y restricciones son parte de las reglas). Se activan con la
/// variable de entorno AYUDAYA_TEST_CONN o con el secreto
/// ConnectionStrings:AyudaYaTest (ver scripts/preparar-db-local.ps1).
/// Sin conexión se omiten. Cada prueba usa datos propios (correos y ciudades
/// con sufijo único) porque la base no permite borrar registros.
/// </summary>
public static class EntornoBaseDeDatos
{
    public static string? Conexion { get; } = LeerConexion();

    private static string? LeerConexion()
    {
        var configuracion = new ConfigurationBuilder()
            .AddUserSecrets(typeof(EntornoBaseDeDatos).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var conexion = configuracion["AYUDAYA_TEST_CONN"] ?? configuracion["ConnectionStrings:AyudaYaTest"];
        return string.IsNullOrWhiteSpace(conexion) ? null : conexion;
    }

    public static ServiceProvider CrearProveedor()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddDataProtection();
        servicios.AddHttpContextAccessor();

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AyudaYa"] = Conexion
            })
            .Build();

        servicios.AddAyudaYa(configuracion);
        return servicios.BuildServiceProvider();
    }
}

/// <summary>[Fact] que se omite si no hay base de datos de pruebas configurada.</summary>
public sealed class FactBaseDeDatosAttribute : FactAttribute
{
    public FactBaseDeDatosAttribute()
    {
        if (EntornoBaseDeDatos.Conexion is null)
        {
            Skip = "Sin base de pruebas: definí AYUDAYA_TEST_CONN (ver scripts/preparar-db-local.ps1).";
        }
    }
}

/// <summary>Escenario armado para cada prueba: servicios, geografía y personas con datos únicos.</summary>
public sealed class Escenario : IAsyncDisposable
{
    public const string Password = "Prueba1234";

    private readonly ServiceProvider _proveedor;
    private readonly AsyncServiceScope _alcance;

    public Escenario()
    {
        _proveedor = EntornoBaseDeDatos.CrearProveedor();
        _alcance = _proveedor.CreateAsyncScope();
        Sufijo = Guid.NewGuid().ToString("N")[..8];
    }

    public string Sufijo { get; }

    public Guid AdminId { get; private set; }

    public Guid CiudadId { get; private set; }

    public Guid ZonaId { get; private set; }

    public Guid OtraZonaId { get; private set; }

    public T Servicio<T>() where T : notnull => _alcance.ServiceProvider.GetRequiredService<T>();

    public async Task PrepararAsync()
    {
        AdminId = await CrearUsuarioAsync("Admin Prueba", administrador: true);

        var admin = Servicio<AdministracionAppService>();
        CiudadId = (await admin.CrearCiudadAsync(AdminId, $"Ciudad {Sufijo}", "Provincia Prueba")).Dato;
        ZonaId = (await admin.CrearZonaAsync(AdminId, CiudadId, "Zona Norte", -34.45, -58.63)).Dato;
        OtraZonaId = (await admin.CrearZonaAsync(AdminId, CiudadId, "Zona Sur", -34.50, -58.60)).Dato;
    }

    /// <summary>Crea un usuario con el correo confirmado.</summary>
    public async Task<Guid> CrearUsuarioAsync(string nombre, bool administrador = false)
    {
        var usuarios = Servicio<UsuarioAppService>();
        var userManager = Servicio<UserManager<AyudaYa.Data.UsuarioIdentity>>();

        var email = $"{nombre.ToLowerInvariant().Replace(' ', '.')}.{Guid.NewGuid():N}@prueba.test";
        var resultado = await usuarios.RegistrarAsync(email, Password, $"{nombre} {Sufijo}");
        Assert.True(resultado.Succeeded, string.Join("; ", resultado.Errors.Select(e => e.Description)));

        var usuario = (await userManager.FindByEmailAsync(email))!;
        usuario.EmailConfirmed = true;
        await userManager.UpdateAsync(usuario);

        if (administrador)
        {
            await userManager.AddToRoleAsync(usuario, UsuarioAppService.RolAdministrador);
        }

        return usuario.Id;
    }

    public async Task<Guid> CrearVoluntarioAsync(string nombre, Guid? zonaId)
    {
        var id = await CrearUsuarioAsync(nombre);
        var resultado = await Servicio<VoluntarioAppService>().InscribirAsync(
            id, new InscribirVoluntarioRequest(CiudadId, zonaId, "011 4444-5555", true));
        Assert.True(resultado.Exito, resultado.Mensajes());
        return id;
    }

    public async Task<IReadOnlyDictionary<string, Guid>> TiposRecursoAsync() =>
        (await Servicio<CatalogosAppService>().TiposRecursoActivosAsync()).ToDictionary(t => t.Codigo, t => t.Id);

    public async Task<IReadOnlyDictionary<string, Guid>> TiposCentroAsync() =>
        (await Servicio<CatalogosAppService>().TiposCentroActivosAsync()).ToDictionary(t => t.Codigo, t => t.Id);

    /// <summary>Crea una solicitud con los recursos indicados (código, personas).</summary>
    public async Task<SolicitudDetalleDto> CrearSolicitudAsync(
        Guid solicitanteId, Guid? zonaId = null, params (string Codigo, int Personas)[] recursos)
    {
        var tipos = await TiposRecursoAsync();
        var resultado = await Servicio<SolicitudAppService>().CrearAsync(
            solicitanteId,
            new CrearSolicitudRequest(
                CiudadId, zonaId ?? ZonaId, "normal", 4, "Descripción de prueba",
                "Calle Falsa 123", "011 5555-0123", -34.451, -58.631,
                recursos.Select(r => new RecursoSolicitadoRequest(tipos[r.Codigo], r.Personas, null)).ToList()));
        Assert.True(resultado.Exito, string.Join(" ", resultado.Errores.Select(e => e.Mensaje)));

        var detalle = await Servicio<SolicitudConsultasAppService>().ObtenerDetalleAsync(
            solicitanteId, resultado.SolicitudId!.Value);
        return detalle.Dato!;
    }

    public async Task<SolicitudDetalleDto> DetalleAsync(Guid actorId, Guid solicitudId)
    {
        var detalle = await Servicio<SolicitudConsultasAppService>().ObtenerDetalleAsync(actorId, solicitudId);
        Assert.True(detalle.Exito, detalle.Mensajes());
        return detalle.Dato!;
    }

    public async ValueTask DisposeAsync()
    {
        await _alcance.DisposeAsync();
        await _proveedor.DisposeAsync();
    }
}

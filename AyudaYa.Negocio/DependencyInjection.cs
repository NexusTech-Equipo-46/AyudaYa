using AyudaYa.Data;
using AyudaYa.Negocio.Identity;
using AyudaYa.Negocio.Servicios;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AyudaYa.Negocio;

/// <summary>
/// Composición de la infraestructura de AyudaYa.
///
/// La capa Web llama a AddAyudaYa y no necesita conocer el DbContext ni las
/// entidades de persistencia. Así los controladores no acceden a Data.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAyudaYa(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AyudaYa");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("AYUDAYA_CONN");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión. Configurá ConnectionStrings:AyudaYa o AYUDAYA_CONN.");
        }

        // Consultas con varias colecciones (Include) en consultas separadas: evita
        // el producto cartesiano y la advertencia de rendimiento de EF Core.
        services.AddDbContext<AyudaYaDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        services
            .AddIdentity<UsuarioIdentity, RolIdentity>(options =>
            {
                // La base exige Email en minúsculas y UserName = Email.
                options.User.RequireUniqueEmail = true;

                // El MVP no envía correos todavía. Si se implementa confirmación
                // por email, cambiar a true y completar el flujo.
                options.SignIn.RequireConfirmedAccount = false;

                // Alineado con el prototipo: 8 caracteres, mayúscula, minúscula
                // y número. Identity por defecto también exige símbolo.
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AyudaYaDbContext>()
            .AddSignInManager<AyudaYaSignInManager>()
            .AddDefaultTokenProviders();

        // La cookie se revalida cada minuto contra SecurityStamp. Al desactivar
        // un usuario se regenera el stamp y la sesión vigente deja de ser válida.
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.FromMinutes(1);
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Cuenta/Login";
            options.LogoutPath = "/Cuenta/Logout";
            options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
            options.Cookie.Name = "AyudaYa.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

        // En contenedores sin disco persistente, las claves de cookies y tokens se
        // guardan en la base para que un reinicio no cierre todas las sesiones.
        if (configuration.GetValue<bool>("DataProtection:PersistirEnBase"))
        {
            services.AddDataProtection()
                .SetApplicationName("AyudaYa")
                .PersistKeysToDbContext<AyudaYaDbContext>();
        }

        services.AddMemoryCache();

        // Correo: SMTP si hay host configurado; si no, el mensaje queda en el log.
        services.Configure<CorreoOptions>(configuration.GetSection(CorreoOptions.Seccion));
        if (string.IsNullOrWhiteSpace(configuration[$"{CorreoOptions.Seccion}:Host"]))
        {
            services.AddSingleton<IEnviadorCorreo, EnviadorCorreoRegistro>();
        }
        else
        {
            services.AddSingleton<IEnviadorCorreo, EnviadorCorreoSmtp>();
        }

        services.AddScoped<UsuarioAppService>();
        services.AddScoped<SolicitudAppService>();
        services.AddScoped<SolicitudConsultasAppService>();
        services.AddScoped<AsignacionAppService>();
        services.AddScoped<VoluntarioAppService>();
        services.AddScoped<OrganizacionAppService>();
        services.AddScoped<AdministracionAppService>();
        services.AddScoped<CatalogosAppService>();
        services.AddScoped<CentrosPublicosAppService>();
        services.AddScoped<MetricasAppService>();
        services.AddScoped<InicializadorAppService>();
        services.AddAuthorization();

        return services;
    }
}

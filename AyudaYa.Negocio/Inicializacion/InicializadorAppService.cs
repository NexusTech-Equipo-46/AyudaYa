using System.Security.Cryptography;
using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AyudaYa.Negocio.Servicios;

/// <summary>
/// Tareas de arranque: crea el primer administrador (si hay credenciales
/// configuradas) y, solo en desarrollo, carga datos de ejemplo para poder
/// recorrer toda la aplicación.
/// </summary>
public sealed class InicializadorAppService
{
    private readonly AyudaYaDbContext _db;
    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly UsuarioAppService _usuarios;
    private readonly CatalogosAppService _catalogos;
    private readonly AdministracionAppService _admin;
    private readonly OrganizacionAppService _organizaciones;
    private readonly VoluntarioAppService _voluntarios;
    private readonly SolicitudAppService _solicitudes;
    private readonly AsignacionAppService _asignaciones;
    private readonly ILogger<InicializadorAppService> _logger;

    public InicializadorAppService(
        AyudaYaDbContext db,
        UserManager<UsuarioIdentity> userManager,
        UsuarioAppService usuarios,
        CatalogosAppService catalogos,
        AdministracionAppService admin,
        OrganizacionAppService organizaciones,
        VoluntarioAppService voluntarios,
        SolicitudAppService solicitudes,
        AsignacionAppService asignaciones,
        ILogger<InicializadorAppService> logger)
    {
        _db = db;
        _userManager = userManager;
        _usuarios = usuarios;
        _catalogos = catalogos;
        _admin = admin;
        _organizaciones = organizaciones;
        _voluntarios = voluntarios;
        _solicitudes = solicitudes;
        _asignaciones = asignaciones;
        _logger = logger;
    }

    /// <summary>
    /// Crea el administrador inicial si todavía no existe ninguno. Las
    /// credenciales vienen de configuración (user-secrets o variables de
    /// entorno), nunca del código.
    /// </summary>
    public async Task<Guid?> AsegurarAdministradorAsync(
        string? email, string? password, string? nombre, CancellationToken cancellationToken = default)
    {
        var existentes = await _userManager.GetUsersInRoleAsync(UsuarioAppService.RolAdministrador);
        var existente = existentes.FirstOrDefault(u => u.Activo);
        if (existente is not null)
        {
            return existente.Id;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "No hay administradores y no se configuró Admin:Email y Admin:Password. " +
                "Configuralos (user-secrets o variables de entorno) para crear el primero.");
            return null;
        }

        var id = await CrearUsuarioAsync(
            email, password, string.IsNullOrWhiteSpace(nombre) ? "Administración AyudaYa" : nombre,
            confirmado: true, cancellationToken);
        if (id is null)
        {
            return null;
        }

        var usuario = await _userManager.FindByIdAsync(id.Value.ToString());
        var resultado = await _userManager.AddToRoleAsync(usuario!, UsuarioAppService.RolAdministrador);
        if (!resultado.Succeeded)
        {
            _logger.LogError("No se pudo asignar el rol Administrador: {Errores}",
                string.Join("; ", resultado.Errors.Select(e => e.Description)));
            return null;
        }

        _logger.LogInformation("Administrador inicial creado: {Email}", email);
        return id;
    }

    /// <summary>
    /// Datos de ejemplo para desarrollo: una ciudad con zonas, organizaciones
    /// y centros aprobados, personas con distintos roles y solicitudes en
    /// distintos estados. Es idempotente: si la ciudad de ejemplo existe, no hace nada.
    /// </summary>
    public async Task<bool> CargarDatosDemoAsync(
        Guid adminId, string passwordDemo, CancellationToken cancellationToken = default)
    {
        if (await _db.Ciudad.AnyAsync(c => c.Nombre == "Tigre" && c.Provincia == "Buenos Aires", cancellationToken))
        {
            return false;
        }

        // --- Geografía ---
        var ciudadId = (await _admin.CrearCiudadAsync(adminId, "Tigre", "Buenos Aires", cancellationToken)).Dato;
        var zonas = new Dictionary<string, Guid>();
        foreach (var (nombre, lat, lon) in new[]
        {
            ("General Pacheco", -34.4550, -58.6330),
            ("El Talar", -34.4740, -58.6500),
            ("Benavídez", -34.4130, -58.6840),
            ("Don Torcuato", -34.4900, -58.6240),
            ("Tigre Centro", -34.4250, -58.5800)
        })
        {
            var zona = await _admin.CrearZonaAsync(adminId, ciudadId, nombre, lat, lon, cancellationToken);
            zonas[nombre] = zona.Dato;
        }

        // --- Personas (todas con correo confirmado) ---
        var marina = await CrearDemoAsync("usuario@ejemplo.test", "Marina Gómez", passwordDemo, "30123456", cancellationToken);
        var lucas = await CrearDemoAsync("voluntario@ejemplo.test", "Lucas Pérez", passwordDemo, null, cancellationToken);
        var camila = await CrearDemoAsync("camila@ejemplo.test", "Camila Rodríguez", passwordDemo, null, cancellationToken);
        var sofia = await CrearDemoAsync("responsable@ejemplo.test", "Sofía Benítez", passwordDemo, null, cancellationToken);
        var julieta = await CrearDemoAsync("miembro@ejemplo.test", "Julieta Ríos", passwordDemo, null, cancellationToken);
        var defensa = await CrearDemoAsync("defensa@ejemplo.test", "Ricardo Salas", passwordDemo, null, cancellationToken);
        var nuevo = await CrearDemoAsync("pendiente@ejemplo.test", "Elena Suárez", passwordDemo, null, cancellationToken);

        await _voluntarios.InscribirAsync(
            lucas, new InscribirVoluntarioRequest(ciudadId, zonas["El Talar"], "011 4444-5555", true), cancellationToken);
        await _voluntarios.InscribirAsync(
            camila, new InscribirVoluntarioRequest(ciudadId, null, "011 4444-6666", true), cancellationToken);

        // --- Organizaciones y centros ---
        var tiposCentro = (await _catalogos.TiposCentroActivosAsync(cancellationToken))
            .ToDictionary(t => t.Codigo, t => t.Id);
        var tiposRecurso = (await _catalogos.TiposRecursoActivosAsync(cancellationToken))
            .ToDictionary(t => t.Codigo, t => t.Id);

        var redSolidaria = (await _organizaciones.RegistrarAsync(sofia, new RegistrarOrganizacionRequest(
            ciudadId, "Red Solidaria Pacheco", "comunitaria", "contacto@redsolidaria.test", "011 4000-1001",
            "Vecinos de General Pacheco que coordinan refugio, abrigo y alimentos durante las emergencias."),
            cancellationToken)).Dato;
        await _admin.CambiarEstadoOrganizacionAsync(adminId, redSolidaria, "activa", null, cancellationToken);
        await _organizaciones.AgregarMiembroAsync(sofia, redSolidaria, "miembro@ejemplo.test", "miembro", cancellationToken);

        var defensaCivil = (await _organizaciones.RegistrarAsync(defensa, new RegistrarOrganizacionRequest(
            ciudadId, "Defensa Civil Tigre", "institucional", "evacuados@defensacivil.test", "011 4000-1003",
            "Coordinación municipal de emergencias."), cancellationToken)).Dato;
        await _admin.CambiarEstadoOrganizacionAsync(adminId, defensaCivil, "activa", null, cancellationToken);

        await _organizaciones.RegistrarAsync(nuevo, new RegistrarOrganizacionRequest(
            ciudadId, "Comedor Manos Unidas", "comunitaria", "manosunidas@ejemplo.test", "011 4000-1008",
            "Viandas de lunes a viernes."), cancellationToken);

        var horarioSemana = new HorarioCentroRequest(false, [1, 2, 3, 4, 5], new TimeOnly(9, 0), new TimeOnly(20, 0));
        var horarioVacio = new HorarioCentroRequest(false, [], null, null);

        async Task<Guid> CentroAsync(
            Guid actor, Guid org, string nombre, string tipo, string zona, string direccion,
            string telefono, double lat, double lon, string descripcion, HorarioCentroRequest horario,
            (string, string)[] recursos, string estadoOperativo = "operativo")
        {
            var creado = await _organizaciones.CrearCentroAsync(actor, org, new GuardarCentroRequest(
                nombre, tiposCentro[tipo], zonas[zona], direccion, telefono, null, lat, lon, descripcion, horario),
                cancellationToken);
            await _admin.CambiarEstadoCentroAsync(adminId, creado.Dato, "aprobado", null, cancellationToken);
            await _organizaciones.ActualizarRecursosAsync(
                actor, creado.Dato, estadoOperativo,
                recursos.ToDictionary(r => r.Item1, r => (string?)r.Item2),
                cancellationToken);
            return creado.Dato;
        }

        await CentroAsync(sofia, redSolidaria, "Refugio Parroquia San José", "refugio", "General Pacheco",
            "Av. Hipólito Yrigoyen 1450, General Pacheco", "011 4000-1001", -34.4588, -58.6365,
            "Alojamiento transitorio para familias evacuadas. Se reciben donaciones de abrigo y artículos de higiene.",
            horarioSemana,
            [("alojamiento", "media"), ("abrigo", "alta"), ("alimentos", "baja"), ("higiene", "media")]);

        await CentroAsync(sofia, redSolidaria, "Comedor Los Abuelos", "comedor", "El Talar",
            "Calle Las Heras 820, El Talar", "011 4000-1002", -34.4715, -58.6542,
            "Almuerzo y merienda todos los días. Hoy estamos al límite de capacidad.",
            new HorarioCentroRequest(false, [1, 2, 3, 4, 5, 6, 7], new TimeOnly(11, 30), new TimeOnly(18, 0)),
            [("alimentos", "baja"), ("agua", "media")], "saturado");

        await CentroAsync(defensa, defensaCivil, "Centro de evacuación Club El Talar", "centro_evacuacion", "El Talar",
            "Av. Constituyentes 2300, El Talar", "011 4000-1003", -34.4762, -58.6481,
            "Gimnasio del club habilitado como centro de evacuación. Atención las 24 horas.",
            new HorarioCentroRequest(true, [], null, null),
            [("alojamiento", "alta"), ("agua", "alta"), ("alimentos", "media"), ("abrigo", "media"),
             ("higiene", "baja"), ("medicamentos", "baja")]);

        await CentroAsync(defensa, defensaCivil, "Hospital Zonal Norte", "hospital", "Don Torcuato",
            "Av. Ángel T. de Alvear 3100, Don Torcuato", "011 4000-1004", -34.4925, -58.6231,
            "Guardia las 24 horas. Para emergencias médicas llamá al 107.",
            new HorarioCentroRequest(true, [], null, null),
            [("medicamentos", "media"), ("traslado", "alta")]);

        await CentroAsync(sofia, redSolidaria, "Punto de acopio Escuela Técnica 2", "punto_donacion", "Benavídez",
            "Av. Benavídez 1230, Benavídez", "011 4000-1006", -34.4108, -58.6893,
            "Recibimos y clasificamos donaciones. Lo más necesario hoy: abrigo para adultos.",
            horarioVacio,
            [("agua", "alta"), ("higiene", "alta"), ("abrigo", "sin_stock"), ("alimentos", "media")]);

        // Un centro todavía sin aprobar para mostrar la cola del administrador.
        await _organizaciones.CrearCentroAsync(sofia, redSolidaria, new GuardarCentroRequest(
            "Comedor Red Solidaria", tiposCentro["comedor"], zonas["General Pacheco"],
            "Calle Los Ceibos 780, General Pacheco", "011 4000-1011", null, -34.4560, -58.6300,
            "Viandas para el mediodía.", horarioSemana), cancellationToken);

        // --- Solicitudes en distintos estados ---
        async Task<Guid> SolicitudAsync(
            Guid solicitante, string zona, string urgencia, int personas, string? descripcion,
            string direccion, double lat, double lon, params (string Codigo, int Personas, string? Detalle)[] recursos)
        {
            var resultado = await _solicitudes.CrearAsync(solicitante, new CrearSolicitudRequest(
                ciudadId, zonas[zona], urgencia, personas, descripcion, direccion, "011 5555-0123", lat, lon,
                recursos.Select(r => new RecursoSolicitadoRequest(tiposRecurso[r.Codigo], r.Personas, r.Detalle)).ToList()),
                cancellationToken);
            return resultado.SolicitudId ?? throw new InvalidOperationException(
                "No se pudo crear una solicitud de ejemplo: " + string.Join(" ", resultado.Errores.Select(e => e.Mensaje)));
        }

        async Task<Guid> RecursoIdAsync(Guid solicitudId, string codigo) =>
            await _db.SolicitudRecurso.Where(r => r.SolicitudId == solicitudId && r.TipoRecurso.Codigo == codigo)
                .Select(r => r.Id).SingleAsync(cancellationToken);

        async Task<Guid> AsignacionIdAsync(Guid solicitudRecursoId) =>
            await _db.Asignacion.Where(a => a.SolicitudRecursoId == solicitudRecursoId
                && (a.Estado == "asignada" || a.Estado == "en_atencion" || a.Estado == "completada"))
                .Select(a => a.Id).SingleAsync(cancellationToken);

        // 1. Pendiente y crítica: nadie la tomó.
        await SolicitudAsync(marina, "General Pacheco", "critica", 4,
            "Casa anegada y sin luz. Hay una persona mayor y un bebé de 8 meses.",
            "Calle Los Ceibos 123, General Pacheco", -34.4540, -58.6310,
            ("alojamiento", 4, "Necesitamos una cama para la persona mayor"), ("abrigo", 4, null));

        // 2. Asignada a un voluntario.
        var s2 = await SolicitudAsync(marina, "El Talar", "normal", 3,
            "Nos quedamos sin mercadería después de la inundación.",
            "Calle Las Heras 455, El Talar", -34.4735, -58.6490,
            ("alimentos", 3, "Una persona es celíaca"), ("higiene", 3, null));
        await _solicitudes.AsignarAsync(lucas, await RecursoIdAsync(s2, "higiene"), lucas, null, cancellationToken);

        // 3. En atención por una organización.
        var s3 = await SolicitudAsync(marina, "El Talar", "normal", 2, null,
            "Av. Constituyentes 1880, El Talar", -34.4770, -58.6470,
            ("alimentos", 2, null));
        await _solicitudes.AsignarAsync(julieta, await RecursoIdAsync(s3, "alimentos"), null, redSolidaria, cancellationToken);
        await _asignaciones.IniciarAsync(julieta, await AsignacionIdAsync(await RecursoIdAsync(s3, "alimentos")), cancellationToken);

        // 4. Resuelta de punta a punta.
        var s4 = await SolicitudAsync(marina, "Don Torcuato", "normal", 5,
            "Familia de cinco con los chicos en la escuela primaria.",
            "Calle Ricardo Rojas 640, Don Torcuato", -34.4890, -58.6200,
            ("abrigo", 5, null));
        var recurso4 = await RecursoIdAsync(s4, "abrigo");
        await _solicitudes.AsignarAsync(camila, recurso4, camila, null, cancellationToken);
        var asignacion4 = await AsignacionIdAsync(recurso4);
        await _asignaciones.IniciarAsync(camila, asignacion4, cancellationToken);
        await _asignaciones.CompletarAsync(camila, asignacion4, cancellationToken);
        await _asignaciones.ConfirmarAsync(marina, asignacion4, cancellationToken);

        // 5. Cancelada por quien la creó.
        var s5 = await SolicitudAsync(marina, "Tigre Centro", "normal", 2, null,
            "Calle Cazón 310, Tigre", -34.4260, -58.5790,
            ("agua", 2, null));
        await _solicitudes.CancelarAsync(marina, s5, "Ya conseguí agua por mi cuenta.", cancellationToken);

        _logger.LogInformation("Datos de ejemplo cargados.");
        return true;
    }

    /// <summary>Contraseña aleatoria con mayúscula, minúscula y número para los datos de ejemplo.</summary>
    public static string GenerarPasswordDemo()
    {
        const string letras = "abcdefghjkmnpqrstuvwxyz";
        const string mayusculas = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string numeros = "23456789";
        var todos = letras + mayusculas + numeros;

        var caracteres = new List<char>
        {
            mayusculas[RandomNumberGenerator.GetInt32(mayusculas.Length)],
            letras[RandomNumberGenerator.GetInt32(letras.Length)],
            numeros[RandomNumberGenerator.GetInt32(numeros.Length)]
        };
        while (caracteres.Count < 14)
        {
            caracteres.Add(todos[RandomNumberGenerator.GetInt32(todos.Length)]);
        }

        return new string(caracteres.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }

    private async Task<Guid> CrearDemoAsync(
        string email, string nombre, string password, string? dni, CancellationToken cancellationToken)
    {
        var id = await CrearUsuarioAsync(email, password, nombre, confirmado: true, cancellationToken, dni);
        return id ?? throw new InvalidOperationException($"No se pudo crear el usuario de ejemplo {email}.");
    }

    private async Task<Guid?> CrearUsuarioAsync(
        string email,
        string password,
        string nombre,
        bool confirmado,
        CancellationToken cancellationToken,
        string? dni = null)
    {
        var resultado = await _usuarios.RegistrarAsync(email, password, nombre, dni, cancellationToken);
        if (!resultado.Succeeded)
        {
            _logger.LogError("No se pudo crear el usuario {Email}: {Errores}", email,
                string.Join("; ", resultado.Errors.Select(e => e.Description)));
            return null;
        }

        var usuario = await _userManager.FindByEmailAsync(UsuarioAppService.NormalizarEmail(email));
        if (usuario is null)
        {
            return null;
        }

        if (confirmado && !usuario.EmailConfirmed)
        {
            usuario.EmailConfirmed = true;
            await _userManager.UpdateAsync(usuario);
        }

        return usuario.Id;
    }
}

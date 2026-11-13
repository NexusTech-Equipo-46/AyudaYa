using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Infraestructura;
using AyudaYa.Web.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

/// <summary>
/// Administración. El rol se exige acá y se vuelve a verificar en cada
/// servicio (defensa en profundidad).
/// </summary>
[Authorize(Roles = UsuarioAppService.RolAdministrador)]
public sealed class AdminController : ControladorAyudaYa
{
    private readonly AdministracionAppService _admin;
    private readonly SolicitudConsultasAppService _consultas;

    public AdminController(AdministracionAppService admin, SolicitudConsultasAppService consultas)
    {
        _admin = admin;
        _consultas = consultas;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var panel = await _admin.ObtenerPanelAsync(UsuarioId, cancellationToken);
        return RespuestaDeError(panel) ?? View(panel.Dato);
    }

    // ------------------------------------------------------------------
    // Ciudades y zonas
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Ciudades(CancellationToken cancellationToken)
    {
        var ciudades = await _admin.ListarCiudadesAsync(UsuarioId, cancellationToken);
        return RespuestaDeError(ciudades) ?? View(ciudades.Dato);
    }

    [HttpPost]
    public async Task<IActionResult> CrearCiudad(string? nombre, string? provincia, CancellationToken cancellationToken)
    {
        var resultado = await _admin.CrearCiudadAsync(UsuarioId, nombre ?? string.Empty, provincia ?? string.Empty, cancellationToken);
        return Redirigir(resultado.Exito ? ResultadoOperacion.Ok() : ResultadoOperacion.Falla(resultado.Errores.ToArray()),
            "Creamos la ciudad. Ahora cargá sus zonas.", nameof(Ciudades));
    }

    [HttpPost]
    public async Task<IActionResult> EstadoCiudad(Guid id, bool activa, CancellationToken cancellationToken) =>
        Redirigir(await _admin.CambiarEstadoCiudadAsync(UsuarioId, id, activa, cancellationToken),
            activa ? "Activamos la ciudad." : "Desactivamos la ciudad.", nameof(Ciudades));

    [HttpPost]
    public async Task<IActionResult> CrearZona(
        Guid ciudadId, string? nombre, double? latitud, double? longitud, CancellationToken cancellationToken)
    {
        var resultado = await _admin.CrearZonaAsync(
            UsuarioId, ciudadId, nombre ?? string.Empty, latitud, longitud, cancellationToken);
        return Redirigir(resultado.Exito ? ResultadoOperacion.Ok() : ResultadoOperacion.Falla(resultado.Errores.ToArray()),
            "Creamos la zona.", nameof(Ciudades));
    }

    [HttpPost]
    public async Task<IActionResult> EditarZona(
        Guid id, string? nombre, double? latitud, double? longitud, bool activa, CancellationToken cancellationToken) =>
        Redirigir(await _admin.ActualizarZonaAsync(
                UsuarioId, id, nombre ?? string.Empty, latitud, longitud, activa, cancellationToken),
            "Guardamos la zona.", nameof(Ciudades));

    // ------------------------------------------------------------------
    // Organizaciones y centros
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Organizaciones(string? estado, CancellationToken cancellationToken)
    {
        var filtro = Filtro(estado, "pendiente", "activa", "rechazada", "suspendida");
        var lista = await _admin.ListarOrganizacionesAsync(UsuarioId, filtro == "todas" ? null : filtro, cancellationToken);
        var denegado = RespuestaDeError(lista);
        if (denegado is not null) return denegado;

        return View(new ListaAdminViewModel<OrganizacionAdminDto> { Elementos = lista.Dato!, Filtro = filtro });
    }

    [HttpPost]
    public async Task<IActionResult> EstadoOrganizacion(
        Guid id, string nuevoEstado, string? motivo, string? filtro, CancellationToken cancellationToken)
    {
        var resultado = await _admin.CambiarEstadoOrganizacionAsync(UsuarioId, id, nuevoEstado, motivo, cancellationToken);
        return Redirigir(resultado, "Guardamos el cambio de estado de la organización.",
            nameof(Organizaciones), valores: new { estado = filtro });
    }

    [HttpGet]
    public async Task<IActionResult> Centros(string? estado, CancellationToken cancellationToken)
    {
        var filtro = Filtro(estado, "pendiente", "aprobado", "rechazado", "suspendido");
        var lista = await _admin.ListarCentrosAsync(UsuarioId, filtro == "todas" ? null : filtro, cancellationToken);
        var denegado = RespuestaDeError(lista);
        if (denegado is not null) return denegado;

        return View(new ListaAdminViewModel<CentroAdminDto> { Elementos = lista.Dato!, Filtro = filtro });
    }

    [HttpPost]
    public async Task<IActionResult> EstadoCentro(
        Guid id, string nuevoEstado, string? motivo, string? filtro, CancellationToken cancellationToken)
    {
        var resultado = await _admin.CambiarEstadoCentroAsync(UsuarioId, id, nuevoEstado, motivo, cancellationToken);
        return Redirigir(resultado, "Guardamos el cambio de estado del centro.",
            nameof(Centros), valores: new { estado = filtro });
    }

    // ------------------------------------------------------------------
    // Usuarios
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Usuarios(string? q, CancellationToken cancellationToken)
    {
        var lista = await _admin.ListarUsuariosAsync(UsuarioId, q, cancellationToken);
        var denegado = RespuestaDeError(lista);
        if (denegado is not null) return denegado;

        return View(new UsuariosAdminViewModel { Usuarios = lista.Dato!, Busqueda = q, UsuarioActualId = UsuarioId });
    }

    [HttpPost]
    public async Task<IActionResult> DesactivarUsuario(Guid id, string? motivo, string? q, CancellationToken cancellationToken) =>
        Redirigir(await _admin.DesactivarUsuarioAsync(UsuarioId, id, motivo ?? string.Empty, cancellationToken),
            "Desactivamos la cuenta. Ya no puede iniciar sesión y se conserva su historial.", nameof(Usuarios), valores: new { q });

    [HttpPost]
    public async Task<IActionResult> ReactivarUsuario(Guid id, string? q, CancellationToken cancellationToken) =>
        Redirigir(await _admin.ReactivarUsuarioAsync(UsuarioId, id, cancellationToken),
            "Reactivamos la cuenta.", nameof(Usuarios), valores: new { q });

    [HttpPost]
    public async Task<IActionResult> RolAdministrador(Guid id, bool esAdministrador, string? q, CancellationToken cancellationToken) =>
        Redirigir(await _admin.CambiarRolAdministradorAsync(UsuarioId, id, esAdministrador, cancellationToken),
            esAdministrador ? "Le dimos el rol de administrador." : "Le quitamos el rol de administrador.",
            nameof(Usuarios), valores: new { q });

    // ------------------------------------------------------------------
    // Solicitudes y auditoría
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Solicitudes(string? estado, CancellationToken cancellationToken)
    {
        var filtro = Filtro(estado, "pendiente", "asignada", "en_atencion", "parcial", "resuelta", "cancelada");
        var lista = await _consultas.ListarTodasAsync(UsuarioId, filtro == "todas" ? null : filtro, 100, cancellationToken);
        var denegado = RespuestaDeError(lista);
        if (denegado is not null) return denegado;

        return View(new ListaAdminViewModel<SolicitudResumenDto> { Elementos = lista.Dato!, Filtro = filtro });
    }

    [HttpGet]
    public async Task<IActionResult> Auditoria(string? entidad, int pagina = 1, CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(pagina, 1);
        var eventos = await _admin.ListarEventosAsync(UsuarioId, entidad, pagina, cancellationToken);
        var denegado = RespuestaDeError(eventos);
        if (denegado is not null) return denegado;

        return View(new AuditoriaViewModel
        {
            Eventos = eventos.Dato!,
            Entidad = entidad,
            Pagina = pagina,
            HayMas = eventos.Dato!.Count == AdministracionAppService.TamanioPaginaAuditoria
        });
    }

    private static string Filtro(string? valor, params string[] permitidos) =>
        valor is not null && permitidos.Contains(valor) ? valor : "todas";
}

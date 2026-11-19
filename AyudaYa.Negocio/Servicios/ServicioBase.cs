using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Negocio.Servicios;

/// <summary>
/// Utilidades comunes de los servicios de aplicación: resolver al usuario que
/// actúa, saber si es administrador y si es miembro activo de una organización.
/// La autorización vive en Negocio; la base solo protege invariantes.
/// </summary>
public abstract class ServicioBase
{
    protected ServicioBase(AyudaYaDbContext db, UserManager<UsuarioIdentity> userManager)
    {
        Db = db;
        UserManager = userManager;
    }

    protected AyudaYaDbContext Db { get; }

    protected UserManager<UsuarioIdentity> UserManager { get; }

    /// <summary>Devuelve el usuario solo si existe y está activo.</summary>
    protected async Task<UsuarioIdentity?> ObtenerActorActivoAsync(Guid actorId)
    {
        var actor = await UserManager.FindByIdAsync(actorId.ToString());
        return actor is { Activo: true } ? actor : null;
    }

    protected Task<bool> EsAdministradorAsync(UsuarioIdentity actor) =>
        UserManager.IsInRoleAsync(actor, UsuarioAppService.RolAdministrador);

    protected Task<bool> EsMiembroActivoAsync(
        Guid usuarioId, Guid organizacionId, CancellationToken cancellationToken) =>
        Db.OrganizacionUsuario.AsNoTracking().AnyAsync(
            ou => ou.OrganizacionId == organizacionId
                && ou.UsuarioId == usuarioId
                && ou.Activo,
            cancellationToken);

    /// <summary>
    /// Miembro activo de una organización que además sigue activa. Se usa para
    /// datos sensibles: una organización suspendida conserva sus membresías,
    /// pero no debe ver ubicaciones exactas ni datos de contacto.
    /// </summary>
    protected Task<bool> EsMiembroActivoDeOrganizacionActivaAsync(
        Guid usuarioId, Guid organizacionId, CancellationToken cancellationToken) =>
        Db.OrganizacionUsuario.AsNoTracking().AnyAsync(
            ou => ou.OrganizacionId == organizacionId
                && ou.UsuarioId == usuarioId
                && ou.Activo
                && ou.Organizacion.Estado == "activa",
            cancellationToken);

    protected Task<bool> EsResponsableActivoAsync(
        Guid usuarioId, Guid organizacionId, CancellationToken cancellationToken) =>
        Db.OrganizacionUsuario.AsNoTracking().AnyAsync(
            ou => ou.OrganizacionId == organizacionId
                && ou.UsuarioId == usuarioId
                && ou.Activo
                && ou.Rol == "responsable",
            cancellationToken);

    /// <summary>
    /// Regla R2: un mismo lugar no se registra dos veces. Busca en la ciudad
    /// otro centro del mismo tipo con la misma dirección normalizada, de
    /// cualquier organización. Un centro rechazado no cuenta, así el lugar se
    /// puede volver a presentar. Se usa al crear, editar y aprobar.
    /// </summary>
    protected async Task<bool> ExisteOtroCentroEnElLugarAsync(
        Guid ciudadId,
        Guid tipoCentroId,
        string direccion,
        Guid? centroActualId,
        CancellationToken cancellationToken)
    {
        // Pocos centros por ciudad y tipo: se comparan en memoria para poder
        // quitar acentos y espacios igual que en ReglasNegocio.
        var direcciones = await Db.Centro.AsNoTracking()
            .Where(c => c.CiudadId == ciudadId
                && c.TipoCentroId == tipoCentroId
                && c.Id != centroActualId
                && c.EstadoVerificacion != "rechazado")
            .Select(c => c.Direccion)
            .ToListAsync(cancellationToken);

        var buscada = ReglasNegocio.NormalizarDireccion(direccion);
        return direcciones.Any(d => ReglasNegocio.NormalizarDireccion(d) == buscada);
    }

    protected static ErrorNegocio CentroYaRegistrado(string mensaje) => new("CentroYaRegistrado", mensaje);

    protected static ErrorNegocio Error(string codigo, string mensaje) => new(codigo, mensaje);

    protected static ErrorNegocio UsuarioInvalido() =>
        new(Codigos.UsuarioInvalido, "El usuario no existe o está inactivo.");

    protected static ErrorNegocio NoAutorizado(string mensaje) =>
        new(Codigos.NoAutorizado, mensaje);

    protected static string? Limpiar(string? valor) => ReglasNegocio.Limpiar(valor);
}

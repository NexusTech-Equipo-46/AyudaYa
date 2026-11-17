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

    protected Task<bool> EsResponsableActivoAsync(
        Guid usuarioId, Guid organizacionId, CancellationToken cancellationToken) =>
        Db.OrganizacionUsuario.AsNoTracking().AnyAsync(
            ou => ou.OrganizacionId == organizacionId
                && ou.UsuarioId == usuarioId
                && ou.Activo
                && ou.Rol == "responsable",
            cancellationToken);

    protected static ErrorNegocio Error(string codigo, string mensaje) => new(codigo, mensaje);

    protected static ErrorNegocio UsuarioInvalido() =>
        new(Codigos.UsuarioInvalido, "El usuario no existe o está inactivo.");

    protected static ErrorNegocio NoAutorizado(string mensaje) =>
        new(Codigos.NoAutorizado, mensaje);

    protected static string? Limpiar(string? valor) => ReglasNegocio.Limpiar(valor);
}

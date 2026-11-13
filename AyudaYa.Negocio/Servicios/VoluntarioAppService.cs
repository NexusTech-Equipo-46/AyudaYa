using AyudaYa.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Negocio.Servicios;

public sealed record VoluntarioDto(
    Guid UsuarioId,
    string Nombre,
    Guid CiudadId,
    string CiudadNombre,
    Guid? ZonaId,
    string? ZonaNombre,
    string? Telefono,
    bool Activo);

public sealed record InscribirVoluntarioRequest(
    Guid CiudadId,
    Guid? ZonaId,
    string Telefono,
    bool AceptaCompartirContacto);

/// <summary>Alta, edición y disponibilidad de voluntarios.</summary>
public sealed class VoluntarioAppService : ServicioBase
{
    public VoluntarioAppService(
        AyudaYaDbContext db,
        UserManager<UsuarioIdentity> userManager)
        : base(db, userManager)
    {
    }

    public Task<VoluntarioDto?> ObtenerAsync(
        Guid usuarioId, CancellationToken cancellationToken = default) =>
        Db.Voluntario.AsNoTracking()
            .Where(v => v.UsuarioId == usuarioId)
            .Select(v => new VoluntarioDto(
                v.UsuarioId,
                v.Usuario.NombreCompleto,
                v.CiudadId,
                v.Ciudad.Nombre,
                v.ZonaId,
                v.Zona == null ? null : v.Zona.Nombre,
                v.TelefonoContacto,
                v.Activo))
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Crea o actualiza el perfil de voluntario del usuario. Si ya existía y
    /// estaba pausado, se conserva el estado de disponibilidad.
    /// </summary>
    public async Task<ResultadoOperacion> InscribirAsync(
        Guid usuarioId,
        InscribirVoluntarioRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var usuario = await ObtenerActorActivoAsync(usuarioId);
        if (usuario is null)
        {
            return ResultadoOperacion.Falla(UsuarioInvalido());
        }

        var errores = new List<ErrorNegocio>();

        if (!usuario.EmailConfirmed)
        {
            errores.Add(Error(
                "EmailNoConfirmado",
                "Tenés que confirmar tu email antes de sumarte como voluntario."));
        }

        if (!solicitud.AceptaCompartirContacto)
        {
            errores.Add(Error(
                "AceptacionRequerida",
                "Tenés que aceptar que compartan tu nombre y teléfono con quien asistas."));
        }

        if (!ReglasNegocio.TelefonoValido(solicitud.Telefono, out var telefono))
        {
            errores.Add(Error(
                "TelefonoInvalido",
                "Escribí un teléfono válido, con característica (entre 8 y 13 dígitos)."));
        }

        if (errores.Count > 0)
        {
            return ResultadoOperacion.Falla(errores.ToArray());
        }

        var ciudad = await Db.Ciudad.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == solicitud.CiudadId, cancellationToken);
        if (ciudad is null || !ciudad.Activa)
        {
            return ResultadoOperacion.Falla(Error(
                "CiudadInvalida", "La ciudad elegida no está disponible."));
        }

        if (solicitud.ZonaId is Guid zonaId)
        {
            var zonaValida = await Db.Zona.AsNoTracking().AnyAsync(
                z => z.Id == zonaId && z.CiudadId == solicitud.CiudadId && z.Activa,
                cancellationToken);
            if (!zonaValida)
            {
                return ResultadoOperacion.Falla(Error(
                    "ZonaInvalida", "La zona elegida no pertenece a esa ciudad o no está activa."));
            }
        }

        var voluntario = await Db.Voluntario
            .SingleOrDefaultAsync(v => v.UsuarioId == usuarioId, cancellationToken);

        if (voluntario is null)
        {
            Db.Voluntario.Add(new Voluntario
            {
                UsuarioId = usuarioId,
                CiudadId = solicitud.CiudadId,
                ZonaId = solicitud.ZonaId,
                TelefonoContacto = telefono,
                Activo = true,
                CreadoAt = DateTime.UtcNow,
                ActualizadoAt = DateTime.UtcNow
            });
        }
        else
        {
            voluntario.CiudadId = solicitud.CiudadId;
            voluntario.ZonaId = solicitud.ZonaId;
            voluntario.TelefonoContacto = telefono;
        }

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Falla(Error(
                Codigos.ConflictoDeEstado, "No se pudieron guardar los datos. Volvé a intentar."));
        }

        return ResultadoOperacion.Ok();
    }

    /// <summary>
    /// Pausa o reanuda la disponibilidad. Una persona en pausa no puede tomar
    /// solicitudes nuevas, pero sus asignaciones vigentes siguen su curso.
    /// </summary>
    public async Task<ResultadoOperacion> CambiarDisponibilidadAsync(
        Guid usuarioId, bool activo, CancellationToken cancellationToken = default)
    {
        var usuario = await ObtenerActorActivoAsync(usuarioId);
        if (usuario is null)
        {
            return ResultadoOperacion.Falla(UsuarioInvalido());
        }

        var voluntario = await Db.Voluntario
            .SingleOrDefaultAsync(v => v.UsuarioId == usuarioId, cancellationToken);
        if (voluntario is null)
        {
            return ResultadoOperacion.Falla(Error(
                "VoluntarioNoEncontrado", "Todavía no te sumaste como voluntario."));
        }

        voluntario.Activo = activo;
        await Db.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion.Ok();
    }
}

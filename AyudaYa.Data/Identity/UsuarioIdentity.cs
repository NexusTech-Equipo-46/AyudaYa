using Microsoft.AspNetCore.Identity;

namespace AyudaYa.Data;

/// <summary>
/// Usuario de la aplicación. Hereda de IdentityUser&lt;Guid&gt; para que
/// ASP.NET Core Identity gestione credenciales, bloqueo y tokens, y agrega
/// solo los datos propios de AyudaYa que existen en "AspNetUsers".
///
/// La tabla sigue llamándose "AspNetUsers" y la clave primaria es uuid.
/// </summary>
public class UsuarioIdentity : IdentityUser<Guid>
{
    /// <summary>Nombre y apellido visibles en la aplicación.</summary>
    public string NombreCompleto { get; set; } = null!;

    /// <summary>
    /// Baja lógica. Un usuario inactivo no puede iniciar sesión ni recuperar
    /// la contraseña, pero sus datos históricos se conservan.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>Motivo obligatorio cuando Activo = false.</summary>
    public string? MotivoDesactivacion { get; set; }

    /// <summary>Fecha UTC de la baja lógica.</summary>
    public DateTime? DesactivadoAt { get; set; }

    /// <summary>Fecha UTC de alta.</summary>
    public DateTime CreadoAt { get; set; }

    public virtual ICollection<Asignacion> AsignacionAsignadaPorNavigation { get; set; } = new List<Asignacion>();

    public virtual ICollection<Asignacion> AsignacionCanceladaPorNavigation { get; set; } = new List<Asignacion>();

    public virtual ICollection<Asignacion> AsignacionCompletadaPorNavigation { get; set; } = new List<Asignacion>();

    public virtual ICollection<Asignacion> AsignacionConfirmadaPorNavigation { get; set; } = new List<Asignacion>();

    public virtual ICollection<Asignacion> AsignacionIniciadaPorNavigation { get; set; } = new List<Asignacion>();

    public virtual ICollection<Asignacion> AsignacionLiberadaPorNavigation { get; set; } = new List<Asignacion>();

    public virtual ICollection<Centro> CentroAprobadoPorNavigation { get; set; } = new List<Centro>();

    public virtual ICollection<Centro> CentroCreadoPorNavigation { get; set; } = new List<Centro>();

    public virtual ICollection<CentroRecurso> CentroRecurso { get; set; } = new List<CentroRecurso>();

    public virtual ICollection<EventoAdministrativo> EventoAdministrativo { get; set; } = new List<EventoAdministrativo>();

    public virtual ICollection<HistorialSolicitud> HistorialSolicitud { get; set; } = new List<HistorialSolicitud>();

    public virtual ICollection<Organizacion> OrganizacionAprobadaPorNavigation { get; set; } = new List<Organizacion>();

    public virtual ICollection<Organizacion> OrganizacionRegistradaPorNavigation { get; set; } = new List<Organizacion>();

    public virtual ICollection<OrganizacionUsuario> OrganizacionUsuarioCreadoPorNavigation { get; set; } = new List<OrganizacionUsuario>();

    /// <summary>
    /// Membresías del usuario en organizaciones. Es 1:N: un usuario puede
    /// pertenecer a varias organizaciones y tener un rol distinto en cada una.
    /// </summary>
    public virtual ICollection<OrganizacionUsuario> OrganizacionUsuarios { get; set; } = new List<OrganizacionUsuario>();

    public virtual ICollection<Solicitud> SolicitudCanceladaPorNavigation { get; set; } = new List<Solicitud>();

    public virtual ICollection<Solicitud> SolicitudSolicitante { get; set; } = new List<Solicitud>();

    /// <summary>DNI y otros datos sensibles, en tabla separada.</summary>
    public virtual UsuarioDatosPrivados? UsuarioDatosPrivados { get; set; }

    /// <summary>Perfil de voluntario, si el usuario decidió serlo.</summary>
    public virtual Voluntario? Voluntario { get; set; }
}

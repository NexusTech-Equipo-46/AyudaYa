using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

/// <summary>
/// Una asignación activa por solicitud_recurso; se conserva el histórico. Exactamente un voluntario o una organización.
/// </summary>
public partial class Asignacion
{
    public Guid Id { get; set; }

    public Guid SolicitudRecursoId { get; set; }

    public Guid? VoluntarioId { get; set; }

    public Guid? OrganizacionId { get; set; }

    public string Estado { get; set; } = null!;

    public Guid AsignadaPor { get; set; }

    public DateTime AsignadaAt { get; set; }

    public Guid? IniciadaPor { get; set; }

    public DateTime? IniciadaAt { get; set; }

    public Guid? CompletadaPor { get; set; }

    public DateTime? CompletadaAt { get; set; }

    public Guid? ConfirmadaPor { get; set; }

    public DateTime? ConfirmadaAt { get; set; }

    public Guid? CanceladaPor { get; set; }

    public DateTime? CanceladaAt { get; set; }

    public string? MotivoCancelacion { get; set; }

    public Guid? LiberadaPor { get; set; }

    public DateTime? LiberadaAt { get; set; }

    public string? MotivoLiberacion { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual UsuarioIdentity AsignadaPorNavigation { get; set; } = null!;

    public virtual UsuarioIdentity? CanceladaPorNavigation { get; set; }

    public virtual UsuarioIdentity? CompletadaPorNavigation { get; set; }

    public virtual UsuarioIdentity? ConfirmadaPorNavigation { get; set; }

    public virtual UsuarioIdentity? IniciadaPorNavigation { get; set; }

    public virtual UsuarioIdentity? LiberadaPorNavigation { get; set; }

    public virtual Organizacion? Organizacion { get; set; }

    public virtual SolicitudRecurso SolicitudRecurso { get; set; } = null!;

    public virtual Voluntario? Voluntario { get; set; }
}

using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class SolicitudRecurso
{
    public Guid Id { get; set; }

    public Guid SolicitudId { get; set; }

    public Guid TipoRecursoId { get; set; }

    /// <summary>
    /// Personas alcanzadas por ESTE recurso. Es la unidad de la métrica; una misma persona puede contarse en más de un recurso.
    /// </summary>
    public int Personas { get; set; }

    public string? Detalle { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime? ResueltaAt { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    /// <summary>
    /// Historial de asignaciones del recurso. Puede haber varias canceladas o
    /// liberadas; la base garantiza una sola activa con ux_asignacion_activa.
    /// </summary>
    public virtual ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();

    public virtual Solicitud Solicitud { get; set; } = null!;

    public virtual TipoRecurso TipoRecurso { get; set; } = null!;
}

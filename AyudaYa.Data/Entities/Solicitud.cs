using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class Solicitud
{
    public Guid Id { get; set; }

    public Guid SolicitanteId { get; set; }

    public Guid CiudadId { get; set; }

    public Guid ZonaId { get; set; }

    public string Urgencia { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public int PersonasAfectadas { get; set; }

    public string? Descripcion { get; set; }

    public string? MotivoCancelacion { get; set; }

    public Guid? CanceladaPor { get; set; }

    public DateTime? CanceladaAt { get; set; }

    public DateTime? AsignadaAt { get; set; }

    public DateTime? EnAtencionAt { get; set; }

    public DateTime? ParcialAt { get; set; }

    public DateTime? ResueltaAt { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual UsuarioIdentity? CanceladaPorNavigation { get; set; }

    public virtual Ciudad Ciudad { get; set; } = null!;

    public virtual ICollection<HistorialSolicitud> HistorialSolicitud { get; set; } = new List<HistorialSolicitud>();

    public virtual UsuarioIdentity Solicitante { get; set; } = null!;

    public virtual SolicitudContacto? SolicitudContacto { get; set; }

    public virtual ICollection<SolicitudRecurso> SolicitudRecurso { get; set; } = new List<SolicitudRecurso>();

    public virtual Zona Zona { get; set; } = null!;
}

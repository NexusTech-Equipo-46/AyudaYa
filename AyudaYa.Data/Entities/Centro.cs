using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class Centro
{
    public Guid Id { get; set; }

    public Guid OrganizacionId { get; set; }

    public Guid CiudadId { get; set; }

    public Guid ZonaId { get; set; }

    public Guid TipoCentroId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string TelefonoContacto { get; set; } = null!;

    public string? EmailContacto { get; set; }

    public double? Latitud { get; set; }

    public double? Longitud { get; set; }

    public string EstadoVerificacion { get; set; } = null!;

    public string EstadoOperativo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? MotivoEstado { get; set; }

    public Guid CreadoPor { get; set; }

    public Guid? AprobadoPor { get; set; }

    public DateTime? AprobadoAt { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual UsuarioIdentity? AprobadoPorNavigation { get; set; }

    public virtual ICollection<CentroHorario> CentroHorario { get; set; } = new List<CentroHorario>();

    public virtual ICollection<CentroRecurso> CentroRecurso { get; set; } = new List<CentroRecurso>();

    public virtual UsuarioIdentity CreadoPorNavigation { get; set; } = null!;

    public virtual Organizacion Organizacion { get; set; } = null!;

    public virtual TipoCentro TipoCentro { get; set; } = null!;

    public virtual Zona Zona { get; set; } = null!;
}

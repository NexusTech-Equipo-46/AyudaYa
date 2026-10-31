using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class Zona
{
    public Guid Id { get; set; }

    public Guid CiudadId { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Activa { get; set; }

    public double? LatitudCentro { get; set; }

    public double? LongitudCentro { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual ICollection<Centro> Centro { get; set; } = new List<Centro>();

    public virtual Ciudad Ciudad { get; set; } = null!;

    public virtual ICollection<Solicitud> Solicitud { get; set; } = new List<Solicitud>();

    public virtual ICollection<Voluntario> Voluntario { get; set; } = new List<Voluntario>();
}

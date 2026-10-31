using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class Ciudad
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Provincia { get; set; } = null!;

    public string Pais { get; set; } = null!;

    public bool Activa { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual ICollection<Organizacion> Organizacion { get; set; } = new List<Organizacion>();

    public virtual ICollection<Solicitud> Solicitud { get; set; } = new List<Solicitud>();

    public virtual ICollection<Voluntario> Voluntario { get; set; } = new List<Voluntario>();

    public virtual ICollection<Zona> Zona { get; set; } = new List<Zona>();
}

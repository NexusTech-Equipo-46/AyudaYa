using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class TipoCentro
{
    public Guid Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual ICollection<Centro> Centro { get; set; } = new List<Centro>();
}

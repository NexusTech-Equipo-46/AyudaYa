using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

/// <summary>
/// Horario semanal de atención de un centro. Varias filas por día permiten
/// varios turnos; sin filas se interpreta como horario no informado.
/// </summary>
public partial class CentroHorario
{
    public Guid Id { get; set; }

    public Guid CentroId { get; set; }

    public short DiaSemana { get; set; }

    public TimeOnly? Abre { get; set; }

    public TimeOnly? Cierra { get; set; }

    public bool TodoElDia { get; set; }

    public string? Notas { get; set; }

    public Guid ActualizadoPor { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual UsuarioIdentity ActualizadoPorNavigation { get; set; } = null!;

    public virtual Centro Centro { get; set; } = null!;
}

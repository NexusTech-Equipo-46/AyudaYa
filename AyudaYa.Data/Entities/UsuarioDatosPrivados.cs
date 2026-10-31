using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

/// <summary>
/// DNI y datos sensibles separados de la tabla principal. Acceso solo administrador o el propio usuario.
/// </summary>
public partial class UsuarioDatosPrivados
{
    public Guid UsuarioId { get; set; }

    public string? Dni { get; set; }

    public DateTime CreadoAt { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public virtual UsuarioIdentity Usuario { get; set; } = null!;
}

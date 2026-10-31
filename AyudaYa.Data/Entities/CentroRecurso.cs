using System;
using System.Collections.Generic;

namespace AyudaYa.Data;

public partial class CentroRecurso
{
    public Guid CentroId { get; set; }

    public Guid TipoRecursoId { get; set; }

    public string Nivel { get; set; } = null!;

    public Guid ActualizadoPor { get; set; }

    public DateTime ActualizadoAt { get; set; }

    public DateTime CreadoAt { get; set; }

    public virtual UsuarioIdentity ActualizadoPorNavigation { get; set; } = null!;

    public virtual Centro Centro { get; set; } = null!;

    public virtual TipoRecurso TipoRecurso { get; set; } = null!;
}

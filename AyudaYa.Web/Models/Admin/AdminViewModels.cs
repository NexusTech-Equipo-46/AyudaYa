using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Web.Models.Admin;

public sealed class ListaAdminViewModel<T>
{
    public required IReadOnlyList<T> Elementos { get; init; }

    public required string Filtro { get; init; }
}

public sealed class UsuariosAdminViewModel
{
    public required IReadOnlyList<UsuarioAdminDto> Usuarios { get; init; }

    public string? Busqueda { get; init; }

    public Guid UsuarioActualId { get; init; }
}

public sealed class AuditoriaViewModel
{
    public required IReadOnlyList<EventoAdminDto> Eventos { get; init; }

    public string? Entidad { get; init; }

    public int Pagina { get; init; }

    public bool HayMas { get; init; }
}

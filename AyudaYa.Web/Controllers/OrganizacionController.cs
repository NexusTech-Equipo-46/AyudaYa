using AyudaYa.Negocio.Servicios;
using AyudaYa.Web.Infraestructura;
using AyudaYa.Web.Models.Organizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AyudaYa.Web.Controllers;

/// <summary>
/// Zona de la organización: panel, solicitudes para tomar, asignaciones,
/// centros, recursos y miembros. La pertenencia se verifica en cada servicio;
/// acá solo se resuelve con qué organización está trabajando la persona.
/// </summary>
[Authorize]
public sealed class OrganizacionController : ControladorAyudaYa
{
    private readonly OrganizacionAppService _organizaciones;
    private readonly SolicitudAppService _solicitudes;
    private readonly SolicitudConsultasAppService _consultas;
    private readonly AsignacionAppService _asignaciones;
    private readonly CatalogosAppService _catalogos;
    private readonly UsuarioAppService _usuarios;

    public OrganizacionController(
        OrganizacionAppService organizaciones,
        SolicitudAppService solicitudes,
        SolicitudConsultasAppService consultas,
        AsignacionAppService asignaciones,
        CatalogosAppService catalogos,
        UsuarioAppService usuarios)
    {
        _organizaciones = organizaciones;
        _solicitudes = solicitudes;
        _consultas = consultas;
        _asignaciones = asignaciones;
        _catalogos = catalogos;
        _usuarios = usuarios;
    }

    // ------------------------------------------------------------------
    // Alta
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Registrar(CancellationToken cancellationToken)
    {
        var modelo = new RegistrarOrganizacionViewModel { Tipo = "comunitaria" };
        return View(await CompletarRegistroAsync(modelo, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(
        RegistrarOrganizacionViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _organizaciones.RegistrarAsync(
                UsuarioId,
                new RegistrarOrganizacionRequest(
                    modelo.CiudadId!.Value,
                    modelo.Nombre ?? string.Empty,
                    modelo.Tipo ?? string.Empty,
                    modelo.EmailContacto ?? string.Empty,
                    modelo.TelefonoContacto ?? string.Empty,
                    modelo.Descripcion),
                cancellationToken);

            if (resultado.Exito)
            {
                Exito("Registramos tu organización. Un administrador la tiene que aprobar antes de que pueda publicar centros y tomar solicitudes.");
                return RedirectToAction(nameof(Panel), new { org = resultado.Dato });
            }

            AgregarErrores(resultado.Errores);
        }

        return View(await CompletarRegistroAsync(modelo, cancellationToken));
    }

    // ------------------------------------------------------------------
    // Panel, solicitudes y asignaciones
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Panel(Guid? org, CancellationToken cancellationToken)
    {
        var (contexto, respuesta) = await ResolverAsync(org, cancellationToken);
        if (respuesta is not null) return respuesta;

        var panel = await _organizaciones.ObtenerPanelAsync(UsuarioId, contexto!.OrganizacionId, cancellationToken);
        var denegado = RespuestaDeError(panel);
        if (denegado is not null) return denegado;

        var asignaciones = await _asignaciones.ListarDeOrganizacionAsync(
            UsuarioId, contexto.OrganizacionId, cancellationToken);

        return View(new PanelOrganizacionViewModel
        {
            Contexto = contexto,
            Panel = panel.Dato!,
            Activas = asignaciones.Dato?.Where(a => a.EsActiva).Take(3).ToList() ?? []
        });
    }

    /// <summary>
    /// Solicitudes de la ciudad de la organización con recursos para tomar,
    /// filtrables por zona, tipo de ayuda y urgencia. Un valor que no está en
    /// los catálogos se ignora.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Solicitudes(
        Guid? org, Guid? zona, string? recurso, string? urgencia, CancellationToken cancellationToken)
    {
        var (contexto, respuesta) = await ResolverAsync(org, cancellationToken);
        if (respuesta is not null) return respuesta;

        var organizacion = await _organizaciones.ObtenerOrganizacionAsync(
            UsuarioId, contexto!.OrganizacionId, cancellationToken);
        var denegado = RespuestaDeError(organizacion);
        if (denegado is not null) return denegado;

        var zonas = await _catalogos.ZonasActivasAsync(organizacion.Dato!.CiudadId, cancellationToken);
        var tiposRecurso = await _catalogos.TiposRecursoActivosAsync(cancellationToken);
        var filtroZona = zonas.Any(z => z.Id == zona) ? zona : null;
        var filtroRecurso = tiposRecurso.Any(t => t.Codigo == recurso) ? recurso : null;
        var filtroUrgencia = urgencia is "critica" or "normal" ? urgencia : null;

        var disponibles = await _consultas.ListarDisponiblesAsync(
            UsuarioId, organizacion.Dato.CiudadId, filtroZona, filtroRecurso, filtroUrgencia, cancellationToken);

        return View(new SolicitudesOrganizacionViewModel
        {
            Contexto = contexto,
            Organizacion = organizacion.Dato,
            Disponibles = disponibles,
            Zonas = zonas,
            TiposRecurso = tiposRecurso,
            FiltroZona = filtroZona,
            FiltroRecurso = filtroRecurso,
            FiltroUrgencia = filtroUrgencia
        });
    }

    [HttpPost]
    public async Task<IActionResult> Tomar(Guid org, Guid recursoId, CancellationToken cancellationToken)
    {
        var resultado = await _solicitudes.AsignarAsync(UsuarioId, recursoId, null, org, cancellationToken);
        return Redirigir(
            resultado,
            "La organización tomó el recurso. Abrilo para ver los datos de contacto y empezar la atención.",
            resultado.Exito ? nameof(Asignaciones) : nameof(Solicitudes),
            valores: new { org });
    }

    [HttpGet]
    public async Task<IActionResult> Asignaciones(Guid? org, string? estado, CancellationToken cancellationToken)
    {
        var (contexto, respuesta) = await ResolverAsync(org, cancellationToken);
        if (respuesta is not null) return respuesta;

        var resultado = await _asignaciones.ListarDeOrganizacionAsync(
            UsuarioId, contexto!.OrganizacionId, cancellationToken);
        var denegado = RespuestaDeError(resultado);
        if (denegado is not null) return denegado;

        var filtro = estado is "cerradas" or "todas" ? estado : "activas";
        var todas = resultado.Dato!;
        var visibles = filtro switch
        {
            "activas" => todas.Where(a => a.EsActiva || a.EsPorConfirmar).ToList(),
            "cerradas" => todas.Where(a => !a.EsActiva && !a.EsPorConfirmar).ToList(),
            _ => todas
        };

        return View(new AsignacionesOrganizacionViewModel
        {
            Contexto = contexto,
            Asignaciones = visibles,
            Filtro = filtro,
            Total = todas.Count
        });
    }

    [HttpGet]
    public async Task<IActionResult> Asignacion(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _asignaciones.ObtenerAsync(UsuarioId, id, cancellationToken);
        return RespuestaDeError(resultado) ?? View(resultado.Dato);
    }

    [HttpPost]
    public async Task<IActionResult> Iniciar(Guid id, Guid? org, CancellationToken cancellationToken) =>
        Redirigir(await _asignaciones.IniciarAsync(UsuarioId, id, cancellationToken),
            "Empezaron la atención.", nameof(Asignacion), valores: new { id, org });

    [HttpPost]
    public async Task<IActionResult> Completar(Guid id, Guid? org, CancellationToken cancellationToken) =>
        Redirigir(await _asignaciones.CompletarAsync(UsuarioId, id, cancellationToken),
            "Marcaron la asignación como completada. Falta que la persona confirme que recibió la ayuda.",
            nameof(Asignacion), valores: new { id, org });

    [HttpPost]
    public async Task<IActionResult> Liberar(Guid id, Guid? org, string? motivo, CancellationToken cancellationToken) =>
        Redirigir(await _asignaciones.LiberarAsync(UsuarioId, id, motivo ?? string.Empty, cancellationToken),
            "Liberaron la asignación: el recurso vuelve a estar pendiente.", nameof(Asignacion), valores: new { id, org });

    // ------------------------------------------------------------------
    // Centros
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Centros(Guid? org, CancellationToken cancellationToken)
    {
        var (contexto, respuesta) = await ResolverAsync(org, cancellationToken);
        if (respuesta is not null) return respuesta;

        var centros = await _organizaciones.ListarCentrosAsync(UsuarioId, contexto!.OrganizacionId, cancellationToken);
        var denegado = RespuestaDeError(centros);
        if (denegado is not null) return denegado;

        return View(new CentrosOrganizacionViewModel { Contexto = contexto, Centros = centros.Dato! });
    }

    [HttpGet]
    public async Task<IActionResult> NuevoCentro(Guid? org, CancellationToken cancellationToken)
    {
        var (contexto, respuesta) = await ResolverAsync(org, cancellationToken);
        if (respuesta is not null) return respuesta;
        if (!contexto!.EsResponsable) return Forbid();

        return View("FormularioCentro", await CompletarCentroAsync(
            new CentroFormularioViewModel { OrganizacionId = contexto.OrganizacionId }, contexto, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> NuevoCentro(
        Guid org, CentroFormularioViewModel modelo, CancellationToken cancellationToken)
    {
        var (contexto, respuesta) = await ResolverAsync(org, cancellationToken);
        if (respuesta is not null) return respuesta;
        modelo.OrganizacionId = contexto!.OrganizacionId;

        if (ModelState.IsValid)
        {
            var resultado = await _organizaciones.CrearCentroAsync(
                UsuarioId, contexto.OrganizacionId, ConvertirCentro(modelo), cancellationToken);

            if (resultado.EsNoAutorizado) return Forbid();
            if (resultado.Exito)
            {
                Exito("Enviamos el centro para su aprobación. Cuando un administrador lo apruebe va a aparecer en el mapa.");
                return RedirectToAction(nameof(Recursos), new { id = resultado.Dato });
            }

            AgregarErrores(resultado.Errores);
        }

        return View("FormularioCentro", await CompletarCentroAsync(modelo, contexto, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> EditarCentro(Guid id, CancellationToken cancellationToken)
    {
        var centro = await _organizaciones.ObtenerCentroAsync(UsuarioId, id, cancellationToken);
        var denegado = RespuestaDeError(centro);
        if (denegado is not null) return denegado;

        var (contexto, respuesta) = await ResolverAsync(centro.Dato!.OrganizacionId, cancellationToken);
        if (respuesta is not null) return respuesta;
        if (!contexto!.EsResponsable) return Forbid();

        var dto = centro.Dato;
        var horarios = dto.Horarios;
        var diasConHorario = horarios.Where(h => !h.TodoElDia).Select(h => h.DiaSemana).Distinct().Order().ToList();
        var primero = horarios.FirstOrDefault(h => !h.TodoElDia);

        var modelo = new CentroFormularioViewModel
        {
            Id = dto.Id,
            OrganizacionId = dto.OrganizacionId,
            Nombre = dto.Nombre,
            TipoCentroId = dto.TipoCentroId,
            ZonaId = dto.ZonaId,
            Direccion = dto.Direccion,
            Telefono = dto.Telefono,
            Email = dto.Email,
            Descripcion = dto.Descripcion,
            Latitud = dto.Latitud,
            Longitud = dto.Longitud,
            TodoElDia = horarios.Count(h => h.TodoElDia) == 7,
            Dias = diasConHorario,
            Abre = primero?.Abre is string abre ? TimeOnly.Parse(abre) : null,
            Cierra = primero?.Cierra is string cierra ? TimeOnly.Parse(cierra) : null,
            EstadoVerificacion = dto.EstadoVerificacion,
            MotivoEstado = dto.MotivoEstado
        };

        return View("FormularioCentro", await CompletarCentroAsync(modelo, contexto, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> EditarCentro(
        Guid id, CentroFormularioViewModel modelo, CancellationToken cancellationToken)
    {
        var centro = await _organizaciones.ObtenerCentroAsync(UsuarioId, id, cancellationToken);
        var denegado = RespuestaDeError(centro);
        if (denegado is not null) return denegado;

        var (contexto, respuesta) = await ResolverAsync(centro.Dato!.OrganizacionId, cancellationToken);
        if (respuesta is not null) return respuesta;

        modelo.Id = id;
        modelo.OrganizacionId = contexto!.OrganizacionId;
        modelo.EstadoVerificacion = centro.Dato.EstadoVerificacion;
        modelo.MotivoEstado = centro.Dato.MotivoEstado;

        if (ModelState.IsValid)
        {
            var resultado = await _organizaciones.ActualizarCentroAsync(
                UsuarioId, id, ConvertirCentro(modelo), cancellationToken);

            if (resultado.EsNoAutorizado) return Forbid();
            if (resultado.Exito)
            {
                Exito(modelo.EsRechazado
                    ? "Volvieron a presentar el centro. Queda pendiente de aprobación hasta que un administrador lo revise."
                    : resultado.Dato
                        ? "Guardamos los cambios. Como cambiaron datos que identifican al centro, vuelve a quedar pendiente de aprobación y no se ve en el mapa hasta que un administrador lo apruebe."
                        : "Guardamos los cambios del centro.");
                return RedirectToAction(nameof(Centros), new { org = contexto.OrganizacionId });
            }

            AgregarErrores(resultado.Errores);
        }

        return View("FormularioCentro", await CompletarCentroAsync(modelo, contexto, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Recursos(Guid id, CancellationToken cancellationToken)
    {
        var centro = await _organizaciones.ObtenerCentroAsync(UsuarioId, id, cancellationToken);
        var denegado = RespuestaDeError(centro);
        if (denegado is not null) return denegado;

        var (contexto, respuesta) = await ResolverAsync(centro.Dato!.OrganizacionId, cancellationToken);
        if (respuesta is not null) return respuesta;

        return View(new RecursosCentroViewModel
        {
            Contexto = contexto!,
            Centro = centro.Dato,
            Tipos = await _catalogos.TiposRecursoActivosAsync(cancellationToken)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Recursos(
        Guid id,
        string estadoOperativo,
        Dictionary<string, string?> niveles,
        CancellationToken cancellationToken)
    {
        var resultado = await _organizaciones.ActualizarRecursosAsync(
            UsuarioId, id, estadoOperativo, niveles ?? [], cancellationToken);

        if (resultado.Errores.Any(e => e.Codigo == Codigos.NoAutorizado)) return Forbid();
        return Redirigir(resultado, "Actualizamos la disponibilidad del centro.", nameof(Recursos), valores: new { id });
    }

    // ------------------------------------------------------------------
    // Miembros
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Miembros(Guid? org, CancellationToken cancellationToken)
    {
        var (contexto, respuesta) = await ResolverAsync(org, cancellationToken);
        if (respuesta is not null) return respuesta;

        var miembros = await _organizaciones.ListarMiembrosAsync(UsuarioId, contexto!.OrganizacionId, cancellationToken);
        var denegado = RespuestaDeError(miembros);
        if (denegado is not null) return denegado;

        return View(new MiembrosViewModel
        {
            Contexto = contexto,
            Miembros = miembros.Dato!,
            UsuarioActualId = UsuarioId
        });
    }

    [HttpPost]
    public async Task<IActionResult> AgregarMiembro(
        Guid org, string? email, string? rol, CancellationToken cancellationToken)
    {
        var resultado = await _organizaciones.AgregarMiembroAsync(
            UsuarioId, org, email ?? string.Empty, rol ?? "miembro", cancellationToken);
        if (resultado.Errores.Any(e => e.Codigo == Codigos.NoAutorizado)) return Forbid();
        return Redirigir(resultado, "Sumamos a la persona a la organización.", nameof(Miembros), valores: new { org });
    }

    [HttpPost]
    public async Task<IActionResult> BajaMiembro(
        Guid org, Guid membresiaId, string? motivo, CancellationToken cancellationToken)
    {
        var resultado = await _organizaciones.DarDeBajaMiembroAsync(
            UsuarioId, org, membresiaId, motivo ?? string.Empty, cancellationToken);
        if (resultado.Errores.Any(e => e.Codigo == Codigos.NoAutorizado)) return Forbid();
        return Redirigir(resultado, "Dimos de baja a la persona. Queda registrada con el motivo.", nameof(Miembros), valores: new { org });
    }

    // ------------------------------------------------------------------
    // Ayudantes
    // ------------------------------------------------------------------

    /// <summary>
    /// Elige con qué organización trabaja la persona: la indicada en la URL
    /// (si es miembro) o, si no, la primera, con los responsables primero.
    /// </summary>
    private async Task<(ContextoOrganizacion? Contexto, IActionResult? Respuesta)> ResolverAsync(
        Guid? org, CancellationToken cancellationToken)
    {
        var membresias = await _organizaciones.ObtenerMembresiasAsync(UsuarioId, cancellationToken);
        if (membresias.Count == 0)
        {
            Error("Todavía no pertenecés a ninguna organización. Podés registrar la tuya.");
            return (null, RedirectToAction(nameof(Registrar)));
        }

        var elegida = org is null ? membresias[0] : membresias.FirstOrDefault(m => m.OrganizacionId == org);
        if (elegida is null)
        {
            return (null, Forbid());
        }

        return (new ContextoOrganizacion
        {
            OrganizacionId = elegida.OrganizacionId,
            Nombre = elegida.OrganizacionNombre,
            Estado = elegida.OrganizacionEstado,
            Rol = elegida.Rol,
            Membresias = membresias
        }, null);
    }

    private async Task<RegistrarOrganizacionViewModel> CompletarRegistroAsync(
        RegistrarOrganizacionViewModel modelo, CancellationToken cancellationToken)
    {
        // Sin el correo confirmado el registro se rechaza: se avisa antes de mostrar el formulario.
        modelo.EmailConfirmado = (await _usuarios.ObtenerPerfilAsync(User))?.EmailConfirmado ?? false;
        if (!modelo.EmailConfirmado)
        {
            return modelo;
        }

        modelo.Ciudades = await _catalogos.CiudadesActivasAsync(cancellationToken);
        if (modelo.CiudadId is null && modelo.Ciudades.Count == 1)
        {
            modelo.CiudadId = modelo.Ciudades[0].Id;
        }

        return modelo;
    }

    private async Task<CentroFormularioViewModel> CompletarCentroAsync(
        CentroFormularioViewModel modelo, ContextoOrganizacion contexto, CancellationToken cancellationToken)
    {
        modelo.Contexto = contexto;
        modelo.TiposCentro = await _catalogos.TiposCentroActivosAsync(cancellationToken);
        var organizacion = await _organizaciones.ObtenerOrganizacionAsync(
            UsuarioId, contexto.OrganizacionId, cancellationToken);
        var ciudadId = organizacion.Dato?.CiudadId;
        modelo.Zonas = await _catalogos.ZonasActivasAsync(ciudadId, cancellationToken);
        return modelo;
    }

    private static GuardarCentroRequest ConvertirCentro(CentroFormularioViewModel m) => new(
        m.Nombre ?? string.Empty,
        m.TipoCentroId ?? Guid.Empty,
        m.ZonaId ?? Guid.Empty,
        m.Direccion ?? string.Empty,
        m.Telefono ?? string.Empty,
        m.Email,
        m.Latitud,
        m.Longitud,
        m.Descripcion,
        new HorarioCentroRequest(m.TodoElDia, m.Dias, m.Abre, m.Cierra));
}

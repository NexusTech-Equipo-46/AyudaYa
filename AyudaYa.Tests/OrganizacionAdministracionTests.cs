using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Tests;

/// <summary>Organizaciones, centros, miembros y administración (con base de datos).</summary>
public class OrganizacionAdministracionTests
{
    private static GuardarCentroRequest Centro(
        Escenario e, Guid tipoCentro, string nombre = "Refugio de prueba", string direccion = "Calle 1") =>
        new(nombre, tipoCentro, e.ZonaId, direccion, "011 4000-1001", null, -34.455, -58.633, null,
            new HorarioCentroRequest(false, [1, 2, 3], new TimeOnly(9, 0), new TimeOnly(18, 0)));

    [FactBaseDeDatos]
    public async Task Una_organizacion_necesita_aprobacion_antes_de_tomar_solicitudes()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var responsable = await e.CrearUsuarioAsync("Responsable");
        var solicitante = await e.CrearUsuarioAsync("Marina");
        var organizaciones = e.Servicio<OrganizacionAppService>();
        var admin = e.Servicio<AdministracionAppService>();
        var solicitudes = e.Servicio<SolicitudAppService>();

        var registro = await organizaciones.RegistrarAsync(responsable, new RegistrarOrganizacionRequest(
            e.CiudadId, $"Red {e.Sufijo}", "comunitaria", "red@prueba.test", "011 4000-1001", null));
        Assert.True(registro.Exito, registro.Mensajes());
        var orgId = registro.Dato;

        // Una persona solo puede ser responsable de una organización.
        var duplicada = await organizaciones.RegistrarAsync(responsable, new RegistrarOrganizacionRequest(
            e.CiudadId, $"Otra {e.Sufijo}", "comunitaria", "otra@prueba.test", "011 4000-1002", null));
        Assert.False(duplicada.Exito);

        var solicitud = await e.CrearSolicitudAsync(solicitante, null, ("alimentos", 2));
        var recurso = solicitud.Recursos.Single();

        // Pendiente: no puede tomar.
        var pendiente = await solicitudes.AsignarAsync(responsable, recurso.Id, null, orgId);
        Assert.False(pendiente.Exito);
        Assert.Equal("OrganizacionInactiva", pendiente.Errores.Single().Codigo);

        // Solo un administrador aprueba.
        var intento = await admin.CambiarEstadoOrganizacionAsync(responsable, orgId, "activa", null);
        Assert.False(intento.Exito);
        Assert.True((await admin.CambiarEstadoOrganizacionAsync(e.AdminId, orgId, "activa", null)).Exito);

        // Aprobada: un tercero que no es miembro no puede asignarla.
        var tercero = await e.CrearUsuarioAsync("Tercero");
        Assert.False((await solicitudes.AsignarAsync(tercero, recurso.Id, null, orgId)).Exito);
        Assert.True((await solicitudes.AsignarAsync(responsable, recurso.Id, null, orgId)).Exito);

        var asignaciones = e.Servicio<AsignacionAppService>();
        var lista = await asignaciones.ListarDeOrganizacionAsync(responsable, orgId);
        Assert.Single(lista.Dato!);

        // Un tercero no ve las asignaciones de la organización.
        Assert.True((await asignaciones.ListarDeOrganizacionAsync(tercero, orgId)).EsNoAutorizado);

        // Rechazar y suspender piden motivo.
        Assert.False((await admin.CambiarEstadoOrganizacionAsync(e.AdminId, orgId, "suspendida", " ")).Exito);
        Assert.True((await admin.CambiarEstadoOrganizacionAsync(e.AdminId, orgId, "suspendida", "Reporte de abuso")).Exito);
    }

    [FactBaseDeDatos]
    public async Task Miembros_roles_y_ultimo_responsable()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var responsable = await e.CrearUsuarioAsync("Responsable");
        var miembro = await e.CrearUsuarioAsync("Miembro");
        var organizaciones = e.Servicio<OrganizacionAppService>();

        var orgId = (await organizaciones.RegistrarAsync(responsable, new RegistrarOrganizacionRequest(
            e.CiudadId, $"Red {e.Sufijo}", "comunitaria", "red@prueba.test", "011 4000-1001", null))).Dato;

        var emailMiembro = (await e.Servicio<UsuarioAppService>().ObtenerPorEmailAsync(
            (await e.Servicio<Microsoft.AspNetCore.Identity.UserManager<AyudaYa.Data.UsuarioIdentity>>()
                .FindByIdAsync(miembro.ToString()))!.Email!))!.Email!;

        // Un miembro no administra el equipo.
        Assert.True((await organizaciones.AgregarMiembroAsync(responsable, orgId, emailMiembro, "miembro")).Exito);
        Assert.False((await organizaciones.AgregarMiembroAsync(responsable, orgId, emailMiembro, "miembro")).Exito);
        var agregarComoMiembro = await organizaciones.AgregarMiembroAsync(miembro, orgId, "x@prueba.test", "miembro");
        Assert.False(agregarComoMiembro.Exito);
        Assert.Equal("NoAutorizado", agregarComoMiembro.Errores.Single().Codigo);

        var miembros = (await organizaciones.ListarMiembrosAsync(responsable, orgId)).Dato!;
        var membresiaResponsable = miembros.Single(m => m.UsuarioId == responsable).MembresiaId;
        var membresiaMiembro = miembros.Single(m => m.UsuarioId == miembro).MembresiaId;

        // El último responsable de una comunitaria no se puede dar de baja.
        var ultimo = await organizaciones.DarDeBajaMiembroAsync(responsable, orgId, membresiaResponsable, "Prueba");
        Assert.False(ultimo.Exito);
        Assert.Equal("UltimoResponsable", ultimo.Errores.Single().Codigo);

        // La baja exige motivo y conserva el registro.
        Assert.False((await organizaciones.DarDeBajaMiembroAsync(responsable, orgId, membresiaMiembro, " ")).Exito);
        Assert.True((await organizaciones.DarDeBajaMiembroAsync(responsable, orgId, membresiaMiembro, "Se mudó")).Exito);
        var tras = (await organizaciones.ListarMiembrosAsync(responsable, orgId)).Dato!;
        var baja = tras.Single(m => m.UsuarioId == miembro);
        Assert.False(baja.Activo);
        Assert.Equal("Se mudó", baja.MotivoBaja);

        // Dada de baja, la persona ya no tiene acceso.
        Assert.True((await organizaciones.ListarMiembrosAsync(miembro, orgId)).EsNoAutorizado);

        // Se puede volver a sumar (se reactiva la misma fila).
        Assert.True((await organizaciones.AgregarMiembroAsync(responsable, orgId, emailMiembro, "miembro")).Exito);
    }

    [FactBaseDeDatos]
    public async Task Un_centro_aprobado_vuelve_a_pendiente_si_cambia_su_identidad()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var responsable = await e.CrearUsuarioAsync("Responsable");
        var miembro = await e.CrearUsuarioAsync("Miembro");
        var organizaciones = e.Servicio<OrganizacionAppService>();
        var admin = e.Servicio<AdministracionAppService>();
        var tiposCentro = await e.TiposCentroAsync();

        var orgId = (await organizaciones.RegistrarAsync(responsable, new RegistrarOrganizacionRequest(
            e.CiudadId, $"Red {e.Sufijo}", "comunitaria", "red@prueba.test", "011 4000-1001", null))).Dato;
        await admin.CambiarEstadoOrganizacionAsync(e.AdminId, orgId, "activa", null);
        var emailMiembro = (await e.Servicio<Microsoft.AspNetCore.Identity.UserManager<AyudaYa.Data.UsuarioIdentity>>()
            .FindByIdAsync(miembro.ToString()))!.Email!;
        await organizaciones.AgregarMiembroAsync(responsable, orgId, emailMiembro, "miembro");

        // Solo un responsable crea centros.
        var intento = await organizaciones.CrearCentroAsync(miembro, orgId, Centro(e, tiposCentro["refugio"]));
        Assert.True(intento.EsNoAutorizado);

        var centroId = (await organizaciones.CrearCentroAsync(responsable, orgId, Centro(e, tiposCentro["refugio"]))).Dato;

        // Nombre duplicado dentro de la organización.
        Assert.False((await organizaciones.CrearCentroAsync(responsable, orgId, Centro(e, tiposCentro["refugio"]))).Exito);

        // Sin aprobar no aparece en el mapa público.
        var publicos = e.Servicio<CentrosPublicosAppService>();
        Assert.Null(await publicos.ObtenerPorIdAsync(centroId));

        Assert.False((await admin.CambiarEstadoCentroAsync(responsable, centroId, "aprobado", null)).Exito);
        Assert.True((await admin.CambiarEstadoCentroAsync(e.AdminId, centroId, "aprobado", null)).Exito);
        var publicado = await publicos.ObtenerPorIdAsync(centroId);
        Assert.NotNull(publicado);
        Assert.Equal(3, publicado!.Horarios.Count);

        // Un miembro actualiza recursos y estado; la fecha se renueva.
        var recursos = new Dictionary<string, string?> { ["agua"] = "alta", ["abrigo"] = "sin_stock", ["alimentos"] = "no_ofrece" };
        var actualizar = await organizaciones.ActualizarRecursosAsync(miembro, centroId, "saturado", recursos);
        Assert.True(actualizar.Exito, actualizar.Mensajes());
        publicado = await publicos.ObtenerPorIdAsync(centroId);
        Assert.Equal("saturado", publicado!.EstadoOperativo);
        Assert.Equal("alta", publicado.Recursos["agua"]);
        Assert.Equal("sin_stock", publicado.Recursos["abrigo"]);
        Assert.False(publicado.Recursos.ContainsKey("alimentos"));
        Assert.False((await organizaciones.ActualizarRecursosAsync(miembro, centroId, "inventado", recursos)).Exito);
        Assert.False((await organizaciones.ActualizarRecursosAsync(
            miembro, centroId, "operativo", new Dictionary<string, string?> { ["agua"] = "enorme" })).Exito);

        // Cambiar el teléfono no afecta la aprobación; cambiar la dirección sí.
        var soloTelefono = Centro(e, tiposCentro["refugio"]) with { Telefono = "011 4000-9999" };
        var cambioMenor = await organizaciones.ActualizarCentroAsync(responsable, centroId, soloTelefono);
        Assert.True(cambioMenor.Exito, cambioMenor.Mensajes());
        Assert.False(cambioMenor.Dato);
        Assert.NotNull(await publicos.ObtenerPorIdAsync(centroId));

        var cambioMayor = await organizaciones.ActualizarCentroAsync(
            responsable, centroId, Centro(e, tiposCentro["refugio"], direccion: "Otra calle 999"));
        Assert.True(cambioMayor.Exito, cambioMayor.Mensajes());
        Assert.True(cambioMayor.Dato);
        Assert.Null(await publicos.ObtenerPorIdAsync(centroId));
    }

    [FactBaseDeDatos]
    public async Task Las_acciones_de_administracion_exigen_rol_y_quedan_auditadas()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var comun = await e.CrearUsuarioAsync("Comun");
        var admin = e.Servicio<AdministracionAppService>();

        // Sin rol no se puede nada.
        Assert.True((await admin.ObtenerPanelAsync(comun)).EsNoAutorizado);
        Assert.False((await admin.CrearCiudadAsync(comun, "Hackeada", "X")).Exito);
        Assert.False((await admin.DesactivarUsuarioAsync(comun, comun, "x")).Exito);

        // Un administrador no se desactiva a sí mismo.
        var autoDesactivar = await admin.DesactivarUsuarioAsync(e.AdminId, e.AdminId, "Prueba");
        Assert.False(autoDesactivar.Exito);
        Assert.Equal("AutoDesactivacion", autoDesactivar.Errores.Single().Codigo);

        // Desactivar exige motivo, bloquea el inicio de sesión y se puede revertir.
        Assert.False((await admin.DesactivarUsuarioAsync(e.AdminId, comun, " ")).Exito);
        Assert.True((await admin.DesactivarUsuarioAsync(e.AdminId, comun, "Spam")).Exito);
        var email = (await e.Servicio<Microsoft.AspNetCore.Identity.UserManager<AyudaYa.Data.UsuarioIdentity>>()
            .FindByIdAsync(comun.ToString()))!.Email!;
        Assert.True((await admin.ReactivarUsuarioAsync(e.AdminId, comun)).Exito);
        Assert.True((await e.Servicio<UsuarioAppService>().ObtenerPorEmailAsync(email))!.Activo);

        // Zonas: nombre repetido y coordenadas incompletas.
        Assert.False((await admin.CrearZonaAsync(e.AdminId, e.CiudadId, "zona norte", null, null)).Exito);
        Assert.False((await admin.CrearZonaAsync(e.AdminId, e.CiudadId, "Zona Este", -34.4, null)).Exito);

        // Cada acción dejó rastro.
        var eventos = (await admin.ListarEventosAsync(e.AdminId, null, 1)).Dato!;
        Assert.Contains(eventos, ev => ev.Entidad == "usuario" && ev.Accion == "desactivar");
        Assert.Contains(eventos, ev => ev.Entidad == "usuario" && ev.Accion == "reactivar");
        Assert.Contains(eventos, ev => ev.Entidad == "zona" && ev.Accion == "crear");
    }
}

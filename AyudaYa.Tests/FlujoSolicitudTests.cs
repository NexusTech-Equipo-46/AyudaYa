using AyudaYa.Negocio.Servicios;

namespace AyudaYa.Tests;

/// <summary>Solicitudes, asignaciones y privacidad de la ubicación (con base de datos).</summary>
public class FlujoSolicitudTests
{
    [FactBaseDeDatos]
    public async Task Una_solicitud_recorre_todo_su_ciclo_y_corta_el_acceso_al_contacto()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var solicitante = await e.CrearUsuarioAsync("Marina");
        var voluntario = await e.CrearVoluntarioAsync("Lucas", e.ZonaId);
        var tercero = await e.CrearUsuarioAsync("Tercero");
        var solicitudes = e.Servicio<SolicitudAppService>();
        var asignaciones = e.Servicio<AsignacionAppService>();

        var solicitud = await e.CrearSolicitudAsync(solicitante, null, ("abrigo", 3), ("agua", 2));
        Assert.Equal("pendiente", solicitud.Estado);
        var abrigo = solicitud.Recursos.Single(r => r.TipoCodigo == "abrigo");

        // Antes de que alguien lo tome nadie más que el solicitante ve la ubicación.
        Assert.False((await solicitudes.ObtenerUbicacionExactaAsync(voluntario, solicitud.Id)).Exito);

        // Toma, privacidad y cambio de estado.
        var tomar = await solicitudes.AsignarAsync(voluntario, abrigo.Id, voluntario, null);
        Assert.True(tomar.Exito, tomar.Mensajes());
        Assert.Equal("asignada", (await e.DetalleAsync(solicitante, solicitud.Id)).Estado);

        Assert.True((await solicitudes.ObtenerUbicacionExactaAsync(voluntario, solicitud.Id)).Exito);
        Assert.True((await solicitudes.ObtenerUbicacionExactaAsync(solicitante, solicitud.Id)).Exito);
        var negado = await solicitudes.ObtenerUbicacionExactaAsync(tercero, solicitud.Id);
        Assert.False(negado.Exito);
        Assert.Equal("NoAutorizado", negado.Errores.Single().Codigo);

        // Iniciar y completar.
        var asignacionId = (await asignaciones.ListarDeVoluntarioAsync(voluntario)).Single().Id;
        Assert.True((await asignaciones.IniciarAsync(voluntario, asignacionId)).Exito);
        Assert.Equal("en_atencion", (await e.DetalleAsync(solicitante, solicitud.Id)).Estado);
        Assert.True((await asignaciones.CompletarAsync(voluntario, asignacionId)).Exito);

        // Mientras falta la confirmación, quien pidió ayuda sigue viendo el teléfono del voluntario.
        var antesDeConfirmar = await e.DetalleAsync(solicitante, solicitud.Id);
        Assert.NotNull(antesDeConfirmar.Recursos.Single(r => r.TipoCodigo == "abrigo").Asignado?.TelefonoContacto);

        // El voluntario no puede confirmar; solo quien pidió ayuda.
        var intruso = await asignaciones.ConfirmarAsync(voluntario, asignacionId);
        Assert.False(intruso.Exito);
        Assert.True((await asignaciones.ConfirmarAsync(solicitante, asignacionId)).Exito);

        // Un recurso resuelto y otro pendiente: atención parcial, y se corta el teléfono de ambos lados.
        var tras = await e.DetalleAsync(solicitante, solicitud.Id);
        Assert.Equal("parcial", tras.Estado);
        Assert.Equal("resuelta", tras.Recursos.Single(r => r.TipoCodigo == "abrigo").Estado);
        Assert.Null(tras.Recursos.Single(r => r.TipoCodigo == "abrigo").Asignado?.TelefonoContacto);

        // Confirmada la atención, el voluntario deja de ver el contacto.
        var cortado = await solicitudes.ObtenerUbicacionExactaAsync(voluntario, solicitud.Id);
        Assert.False(cortado.Exito);
        var detalleAsignacion = await asignaciones.ObtenerAsync(voluntario, asignacionId);
        Assert.True(detalleAsignacion.Exito);
        Assert.Null(detalleAsignacion.Dato!.Contacto);
        Assert.Equal("confirmada", detalleAsignacion.Dato.Estado);

        // El impacto cuenta a las 3 personas del recurso resuelto.
        var impacto = await e.Servicio<MetricasAppService>().ObtenerImpactoAsync(e.CiudadId);
        Assert.Equal(3, impacto.TotalPersonas);

        // Cancelar cierra el resto.
        var cancelar = await solicitudes.CancelarAsync(solicitante, solicitud.Id, "Ya no lo necesito");
        Assert.True(cancelar.Exito, cancelar.Mensajes());
        Assert.Equal("cancelada", (await e.DetalleAsync(solicitante, solicitud.Id)).Estado);
    }

    [FactBaseDeDatos]
    public async Task Liberar_devuelve_el_recurso_a_pendiente_y_otro_voluntario_puede_tomarlo()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var solicitante = await e.CrearUsuarioAsync("Marina");
        var primero = await e.CrearVoluntarioAsync("Primero", e.ZonaId);
        var segundo = await e.CrearVoluntarioAsync("Segundo", null);
        var solicitudes = e.Servicio<SolicitudAppService>();
        var asignaciones = e.Servicio<AsignacionAppService>();

        var solicitud = await e.CrearSolicitudAsync(solicitante, null, ("alimentos", 4));
        var recurso = solicitud.Recursos.Single();

        Assert.True((await solicitudes.AsignarAsync(primero, recurso.Id, primero, null)).Exito);

        // Un recurso ya tomado no puede tomarlo otro.
        Assert.False((await solicitudes.AsignarAsync(segundo, recurso.Id, segundo, null)).Exito);

        var asignacionId = (await asignaciones.ListarDeVoluntarioAsync(primero)).Single().Id;

        // Liberar exige motivo.
        Assert.False((await asignaciones.LiberarAsync(primero, asignacionId, "  ")).Exito);
        Assert.True((await asignaciones.LiberarAsync(primero, asignacionId, "No llego a tiempo")).Exito);

        var detalle = await e.DetalleAsync(solicitante, solicitud.Id);
        Assert.Equal("pendiente", detalle.Estado);
        Assert.Equal("pendiente", detalle.Recursos.Single().Estado);

        // El que liberó ya no ve el contacto; el otro lo toma.
        Assert.False((await solicitudes.ObtenerUbicacionExactaAsync(primero, solicitud.Id)).Exito);
        Assert.True((await solicitudes.AsignarAsync(segundo, recurso.Id, segundo, null)).Exito);
        Assert.True((await solicitudes.ObtenerUbicacionExactaAsync(segundo, solicitud.Id)).Exito);
    }

    [FactBaseDeDatos]
    public async Task Reglas_de_quien_puede_tomar_una_solicitud()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var solicitante = await e.CrearVoluntarioAsync("Solicitante Voluntario", e.ZonaId);
        var deOtraZona = await e.CrearVoluntarioAsync("Otra Zona", e.OtraZonaId);
        var solicitudes = e.Servicio<SolicitudAppService>();
        var voluntarios = e.Servicio<VoluntarioAppService>();

        var solicitud = await e.CrearSolicitudAsync(solicitante, null, ("agua", 2));
        var recurso = solicitud.Recursos.Single();

        // No se puede tomar la propia solicitud.
        var propia = await solicitudes.AsignarAsync(solicitante, recurso.Id, solicitante, null);
        Assert.False(propia.Exito);
        Assert.Equal("SolicitudPropia", propia.Errores.Single().Codigo);

        // Un voluntario de otra ciudad no la toma.
        var otraCiudadId = (await e.Servicio<AdministracionAppService>()
            .CrearCiudadAsync(e.AdminId, $"Otra Ciudad {e.Sufijo}", "Provincia Prueba")).Dato;
        var deOtraCiudad = await e.CrearUsuarioAsync("Otra Ciudad");
        Assert.True((await voluntarios.InscribirAsync(
            deOtraCiudad, new InscribirVoluntarioRequest(otraCiudadId, null, "011 4444-7777", true))).Exito);
        var otraCiudad = await solicitudes.AsignarAsync(deOtraCiudad, recurso.Id, deOtraCiudad, null);
        Assert.False(otraCiudad.Exito);
        Assert.Equal("VoluntarioOtraCiudad", otraCiudad.Errores.Single().Codigo);

        // Un voluntario de otra zona de la misma ciudad sí puede tomarla (ej. al ampliar la búsqueda).
        var solicitudOtraZona = await e.CrearSolicitudAsync(solicitante, null, ("agua", 1));
        var otraZona = await solicitudes.AsignarAsync(
            deOtraZona, solicitudOtraZona.Recursos.Single().Id, deOtraZona, null);
        Assert.True(otraZona.Exito, otraZona.Mensajes());

        // Un voluntario sin zona (toda la ciudad) sí, salvo que esté en pausa.
        var sinZona = await e.CrearVoluntarioAsync("Sin Zona", null);
        await voluntarios.CambiarDisponibilidadAsync(sinZona, false);
        Assert.False((await solicitudes.AsignarAsync(sinZona, recurso.Id, sinZona, null)).Exito);
        await voluntarios.CambiarDisponibilidadAsync(sinZona, true);
        Assert.True((await solicitudes.AsignarAsync(sinZona, recurso.Id, sinZona, null)).Exito);

        // Un tercero no puede asignar a otra persona.
        var otro = await e.CrearVoluntarioAsync("Otro", null);
        var solicitud2 = await e.CrearSolicitudAsync(solicitante, null, ("agua", 1));
        var ajeno = await solicitudes.AsignarAsync(otro, solicitud2.Recursos.Single().Id, sinZona, null);
        Assert.False(ajeno.Exito);
        Assert.Equal("NoAutorizado", ajeno.Errores.Single().Codigo);
    }

    [FactBaseDeDatos]
    public async Task Los_disponibles_no_incluyen_las_propias_ni_los_datos_de_contacto()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var solicitante = await e.CrearUsuarioAsync("Marina");
        var voluntario = await e.CrearVoluntarioAsync("Lucas", e.ZonaId);
        var consultas = e.Servicio<SolicitudConsultasAppService>();

        var solicitud = await e.CrearSolicitudAsync(solicitante, null, ("agua", 2));

        var paraVoluntario = await consultas.ListarDisponiblesAsync(voluntario, e.CiudadId, e.ZonaId);
        Assert.Contains(paraVoluntario, s => s.Id == solicitud.Id);

        var paraElMismo = await consultas.ListarDisponiblesAsync(solicitante, e.CiudadId, null);
        Assert.DoesNotContain(paraElMismo, s => s.Id == solicitud.Id);

        // En otra zona no aparece.
        var otraZona = await consultas.ListarDisponiblesAsync(voluntario, e.CiudadId, e.OtraZonaId);
        Assert.DoesNotContain(otraZona, s => s.Id == solicitud.Id);

        // Un tercero no puede ver el detalle de una solicitud ajena.
        var ajeno = await consultas.ObtenerDetalleAsync(voluntario, solicitud.Id);
        Assert.False(ajeno.Exito);
        Assert.True(ajeno.EsNoAutorizado);
    }

    [FactBaseDeDatos]
    public async Task Las_urgencias_se_guardan_filtran_y_ordenan_por_prioridad()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();
        var solicitante = await e.CrearUsuarioAsync("Marina");
        var voluntario = await e.CrearVoluntarioAsync("Lucas", e.ZonaId);
        var solicitudes = e.Servicio<SolicitudAppService>();
        var consultas = e.Servicio<SolicitudConsultasAppService>();
        var agua = (await e.TiposRecursoAsync())["agua"];
        var ids = new Dictionary<string, Guid>();

        foreach (var urgencia in new[] { "normal", "critica" })
        {
            var pedido = new CrearSolicitudRequest(
                e.CiudadId, e.ZonaId, urgencia, 1, null, "Calle 1", "011 5555-0123", -34.4, -58.6,
                [new RecursoSolicitadoRequest(agua, 1, null)]);
            var resultado = await solicitudes.CrearAsync(solicitante, pedido);
            Assert.True(resultado.Exito, string.Join(" ", resultado.Errores.Select(error => error.Mensaje)));
            ids[urgencia] = resultado.SolicitudId!.Value;
        }

        var disponibles = await consultas.ListarDisponiblesAsync(voluntario, e.CiudadId, e.ZonaId);
        Assert.Equal(new[] { "critica", "normal" }, disponibles.Select(s => s.Urgencia));

        foreach (var urgencia in ids.Keys)
        {
            var filtradas = await consultas.ListarDisponiblesAsync(
                voluntario, e.CiudadId, e.ZonaId, urgencia: urgencia);
            Assert.Equal(ids[urgencia], Assert.Single(filtradas).Id);
        }
    }

    [FactBaseDeDatos]
    public async Task Los_disponibles_se_filtran_por_tipo_de_ayuda_y_urgencia()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var solicitante = await e.CrearUsuarioAsync("Marina");
        var voluntario = await e.CrearVoluntarioAsync("Lucas", null);
        var consultas = e.Servicio<SolicitudConsultasAppService>();

        // La solicitud de prueba es de urgencia normal.
        var solicitud = await e.CrearSolicitudAsync(solicitante, null, ("agua", 2));

        Assert.Contains(await consultas.ListarDisponiblesAsync(voluntario, e.CiudadId, null, "agua"), s => s.Id == solicitud.Id);
        Assert.DoesNotContain(await consultas.ListarDisponiblesAsync(voluntario, e.CiudadId, null, "abrigo"), s => s.Id == solicitud.Id);
        Assert.Contains(await consultas.ListarDisponiblesAsync(voluntario, e.CiudadId, null, urgencia: "normal"), s => s.Id == solicitud.Id);
        Assert.DoesNotContain(await consultas.ListarDisponiblesAsync(voluntario, e.CiudadId, null, urgencia: "critica"), s => s.Id == solicitud.Id);
    }

    [FactBaseDeDatos]
    public async Task Un_usuario_desactivado_o_sin_confirmar_no_puede_pedir_ayuda()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();

        var usuarios = e.Servicio<UsuarioAppService>();
        var sinConfirmar = $"sin.confirmar.{Guid.NewGuid():N}@prueba.test";
        Assert.True((await usuarios.RegistrarAsync(sinConfirmar, Escenario.Password, "Sin Confirmar")).Succeeded);
        var usuario = await usuarios.ObtenerPorEmailAsync(sinConfirmar);

        var tipos = await e.TiposRecursoAsync();
        var solicitud = new CrearSolicitudRequest(
            e.CiudadId, e.ZonaId, "normal", 1, null, "Calle 1", "011 5555-0123", -34.4, -58.6,
            [new RecursoSolicitadoRequest(tipos["agua"], 1, null)]);

        var resultado = await e.Servicio<SolicitudAppService>().CrearAsync(usuario!.Id, solicitud);
        Assert.False(resultado.Exito);
        Assert.Equal("EmailNoConfirmado", resultado.Errores.Single().Codigo);

        // Desactivado: tampoco.
        var activo = await e.CrearUsuarioAsync("Activo");
        Assert.True((await e.Servicio<AdministracionAppService>().DesactivarUsuarioAsync(e.AdminId, activo, "Prueba")).Exito);
        var desactivado = await e.Servicio<SolicitudAppService>().CrearAsync(activo, solicitud);
        Assert.False(desactivado.Exito);
        Assert.Equal("UsuarioInactivo", desactivado.Errores.Single().Codigo);
    }

    [FactBaseDeDatos]
    public async Task La_solicitud_se_valida_antes_de_guardar()
    {
        await using var e = new Escenario();
        await e.PrepararAsync();
        var solicitante = await e.CrearUsuarioAsync("Marina");
        var tipos = await e.TiposRecursoAsync();
        var solicitudes = e.Servicio<SolicitudAppService>();

        CrearSolicitudRequest Pedido(
            int personas = 4, string telefono = "011 5555-0123", double? lat = -34.4, double? lon = -58.6,
            int personasRecurso = 2, Guid? zona = null) =>
            new(e.CiudadId, zona ?? e.ZonaId, "normal", personas, null, "Calle 1", telefono, lat, lon,
                [new RecursoSolicitadoRequest(tipos["agua"], personasRecurso, null)]);

        Assert.False((await solicitudes.CrearAsync(solicitante, Pedido(personas: 0))).Exito);
        Assert.False((await solicitudes.CrearAsync(solicitante, Pedido(telefono: "123"))).Exito);
        Assert.False((await solicitudes.CrearAsync(solicitante, Pedido(lat: null))).Exito);
        Assert.False((await solicitudes.CrearAsync(solicitante, Pedido(lat: 123))).Exito);
        Assert.False((await solicitudes.CrearAsync(solicitante, Pedido(personasRecurso: 9))).Exito);
        Assert.False((await solicitudes.CrearAsync(solicitante, Pedido(zona: Guid.NewGuid()))).Exito);
        Assert.True((await solicitudes.CrearAsync(solicitante, Pedido())).Exito);
    }
}

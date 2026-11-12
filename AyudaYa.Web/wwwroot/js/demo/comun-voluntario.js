// Funciones compartidas por las pantallas de la zona Voluntario.
// Va después de comun.js y reutiliza window.AyudaYa.
(function () {
    "use strict";

    const A = window.AyudaYa;
    const D = window.DATOS_VOLUNTARIO;

    const ic = (trazos) => '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false">' + trazos + "</svg>";
    const trazo = 'fill="none" stroke="currentColor" stroke-width="1.6"';

    // Cada estado tiene una forma de ícono propia, no solo un color.
    const iconos = {
        asignada: ic('<rect x="3.5" y="2.5" width="9" height="11.5" rx="1.5" ' + trazo + '/><path d="M6 2.5h4v2H6z" ' + trazo + ' stroke-linejoin="round"/><path d="M5.8 8h4.4M5.8 10.5h3" ' + trazo + ' stroke-linecap="round"/>'),
        en_atencion: ic('<circle cx="8" cy="8" r="6.5" ' + trazo + '/><path d="M6.8 5.6v4.8L10.6 8z" fill="currentColor"/>'),
        completada: ic('<circle cx="8" cy="8" r="6.5" ' + trazo + '/><path d="M5 8.3l2.2 2.2L11 6" ' + trazo + ' stroke-linecap="round" stroke-linejoin="round"/>'),
        confirmada: ic('<circle cx="8" cy="8" r="7" fill="currentColor"/><path d="M5 8.3l2.2 2.2L11 6" fill="none" stroke="#3F6212" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>'),
        liberada: ic('<path d="M6 4L3 7l3 3" ' + trazo + ' stroke-linecap="round" stroke-linejoin="round"/><path d="M3 7h6.5a3.5 3.5 0 0 1 0 7H7" ' + trazo + ' stroke-linecap="round"/>'),
        candado: ic('<rect x="3" y="7" width="10" height="7" rx="1.5" ' + trazo + '/><path d="M5.2 7V5a2.8 2.8 0 0 1 5.6 0v2" ' + trazo + '/>')
    };

    const estados = {
        asignada: { texto: "Asignada", icono: iconos.asignada },
        en_atencion: { texto: "En atención", icono: iconos.en_atencion },
        completada: { texto: "Completada", icono: iconos.completada },
        confirmada: { texto: "Confirmada", icono: iconos.confirmada },
        cancelada: { texto: "Cancelada", icono: A.iconos.cruz },
        liberada: { texto: "Liberada", icono: iconos.liberada }
    };

    // "Confirmada" = completada + confirmada_at.
    const estadoVisible = (a) => (a.estado === "completada" && a.confirmada_at ? "confirmada" : a.estado);
    const esActiva = (a) => a.estado === "asignada" || a.estado === "en_atencion";
    const esPorConfirmar = (a) => a.estado === "completada" && !a.confirmada_at;
    // Mientras la tarea esté viva (o sin confirmar) ve el contacto; después ya no.
    const veContacto = (a) => esActiva(a) || esPorConfirmar(a);

    const plural = (n, uno, varios) => n + " " + (n === 1 ? uno : varios);
    const nombreCorto = (nombre) => {
        const p = nombre.trim().split(/\s+/);
        return p.length > 1 ? p[0] + " " + p[1][0] + "." : p[0];
    };

    function insigniaAsignacion(a) {
        const clave = estadoVisible(a);
        const e = estados[clave];
        return '<span class="insignia asig-' + clave + '">' + e.icono + '<span class="visualmente-oculto">Estado: </span>' + e.texto + "</span>";
    }

    function insigniaUrgencia(urgencia) {
        return urgencia === "critica"
            ? '<span class="insignia urgencia-critica">' + A.iconos.exclamacion + '<span class="visualmente-oculto">Urgencia: </span>Crítica</span>'
            : '<span class="insignia urgencia-normal">' + A.iconos.guion + '<span class="visualmente-oculto">Urgencia: </span>Normal</span>';
    }

    function tituloAsignacion(a) {
        return A.nombreRecurso(a.recurso.tipo_recurso) + " para " + plural(a.recurso.personas, "persona", "personas");
    }

    function siguientePaso(a) {
        const quien = A.escapar(nombreCorto(a.solicitante_nombre));
        switch (estadoVisible(a)) {
            case "asignada": return "Próximo paso: comunicate con " + quien + " y empezá la atención.";
            case "en_atencion": return "Próximo paso: cuando termines, marcala como completada.";
            case "completada": return "Esperando que " + quien + " confirme que recibió la ayuda.";
            case "confirmada": return "Terminada. " + quien + " confirmó la ayuda. ¡Gracias!";
            case "cancelada": return "La solicitud se canceló: no hace falta que hagas nada.";
            default: return "La liberaste: otra persona puede tomarla.";
        }
    }

    function tarjetaAsignacion(a, opciones) {
        opciones = opciones || {};
        const nivel = opciones.nivelTitulo || "h3";
        const clases = "tarjeta asignacion" +
            (esActiva(a) && a.solicitud.urgencia === "critica" ? " es-critica" : (esActiva(a) ? " necesita-accion" : ""));
        return '<article class="' + clases + '" aria-labelledby="titulo-' + a.id + '">' +
            '<div class="cabeza">' +
                "<" + nivel + ' id="titulo-' + a.id + '"><a href="' + A.escapar(document.body.dataset.urlAsignacionVoluntario) + '?id=' + encodeURIComponent(a.id) + '">' + A.escapar(tituloAsignacion(a)) + "</a></" + nivel + ">" +
                '<div class="insignias">' + insigniaAsignacion(a) + (esActiva(a) && a.solicitud.urgencia === "critica" ? insigniaUrgencia("critica") : "") + "</div>" +
            "</div>" +
            '<p class="meta">Solicitud #' + a.solicitud.numero + " · " + A.escapar(a.solicitud.zona_nombre) + " · " +
                plural(a.solicitud.personas_afectadas, "persona afectada", "personas afectadas") + "</p>" +
            '<p class="siguiente">' + siguientePaso(a) + "</p>" +
            '<div class="pie-asignacion"><span>Tomada <time datetime="' + a.asignada_at + '" title="' + A.fechaCompleta(a.asignada_at) + '">' + A.haceCuanto(a.asignada_at) + "</time></span>" +
                '<a href="' + A.escapar(document.body.dataset.urlAsignacionVoluntario) + '?id=' + encodeURIComponent(a.id) + '">Ver detalle<span class="visualmente-oculto"> de ' + A.escapar(tituloAsignacion(a)) + "</span></a></div>" +
        "</article>";
    }

    // Línea de tiempo a partir de las columnas *_at de la asignación (más reciente primero).
    function historialDe(a) {
        const eventos = [{ estado: "asignada", actor: "Vos", cuando: a.asignada_at, motivo: null }];
        if (a.iniciada_at) eventos.push({ estado: "en_atencion", actor: "Vos", cuando: a.iniciada_at, motivo: null });
        if (a.completada_at) eventos.push({ estado: "completada", actor: "Vos", cuando: a.completada_at, motivo: null });
        if (a.confirmada_at) eventos.push({ estado: "confirmada", actor: a.solicitante_nombre, cuando: a.confirmada_at, motivo: null });
        if (a.cancelada_at) eventos.push({ estado: "cancelada", actor: a.cancelada_por_nombre || "Administración", cuando: a.cancelada_at, motivo: a.motivo_cancelacion });
        if (a.liberada_at) eventos.push({ estado: "liberada", actor: "Vos", cuando: a.liberada_at, motivo: a.motivo_liberacion });
        return eventos.sort((x, y) => new Date(y.cuando) - new Date(x.cuando));
    }

    function insigniaEvento(clave) {
        const e = estados[clave];
        return '<span class="insignia asig-' + clave + '">' + e.icono + e.texto + "</span>";
    }

    window.AyudaYaVoluntario = {
        iconos, estados, estadoVisible, esActiva, esPorConfirmar, veContacto,
        insigniaAsignacion, insigniaUrgencia, insigniaEvento, tituloAsignacion, siguientePaso,
        tarjetaAsignacion, historialDe, plural, nombreCorto,
        voluntario: D.voluntario
    };
})();

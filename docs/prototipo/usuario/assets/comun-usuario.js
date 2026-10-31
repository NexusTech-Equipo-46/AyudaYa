// Funciones compartidas por las pantallas de la zona Usuario autenticado.
// Va después de comun.js y reutiliza window.AyudaYa.
(function () {
    "use strict";

    const A = window.AyudaYa;
    const DU = window.DATOS_USUARIO;

    const ic = (trazos) => '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false">' + trazos + "</svg>";
    const trazo = 'fill="none" stroke="currentColor" stroke-width="1.6"';

    // Íconos nuevos. Cada estado tiene forma propia, no solo color.
    const iconos = {
        reloj: ic('<circle cx="8" cy="8" r="6.5" ' + trazo + '/><path d="M8 4.5V8l2.5 1.5" ' + trazo + ' stroke-linecap="round"/>'),
        persona: ic('<circle cx="8" cy="5" r="2.6" ' + trazo + '/><path d="M2.8 14c.4-3 2.5-4.6 5.2-4.6s4.8 1.6 5.2 4.6" ' + trazo + ' stroke-linecap="round"/>'),
        corazon: ic('<path d="M8 13.5S2 10 2 6a3 3 0 0 1 6-1 3 3 0 0 1 6 1c0 4-6 7.5-6 7.5z" ' + trazo + ' stroke-linejoin="round"/>'),
        mitad: ic('<circle cx="8" cy="8" r="6.5" ' + trazo + '/><path d="M8 1.5a6.5 6.5 0 0 1 0 13z" fill="currentColor"/>'),
        candado: ic('<rect x="3" y="7" width="10" height="7" rx="1.5" ' + trazo + '/><path d="M5.2 7V5a2.8 2.8 0 0 1 5.6 0v2" ' + trazo + '/>')
    };

    // Valores de solicitud.estado y solicitud_recurso.estado.
    const estados = {
        pendiente: { texto: "Pendiente", icono: iconos.reloj, descripcion: "Todavía no la tomó nadie." },
        asignada: { texto: "Asignada", icono: iconos.persona, descripcion: "Alguien la tomó y se va a comunicar con vos." },
        en_atencion: { texto: "En atención", icono: iconos.corazon, descripcion: "Te están asistiendo." },
        parcial: { texto: "Atención parcial", icono: iconos.mitad, descripcion: "Una parte ya se resolvió y otra sigue en espera." },
        resuelta: { texto: "Resuelta", icono: A.iconos.check, descripcion: "Se resolvió lo que pediste." },
        cancelada: { texto: "Cancelada", icono: A.iconos.cruz, descripcion: "La solicitud se canceló." }
    };

    const abiertas = ["pendiente", "asignada", "en_atencion", "parcial"];
    const esAbierta = (s) => abiertas.includes(s.estado);

    function insigniaSolicitud(estado) {
        const e = estados[estado];
        return '<span class="insignia sol-' + estado + '">' + e.icono + '<span class="visualmente-oculto">Estado: </span>' + e.texto + "</span>";
    }

    function insigniaUrgencia(urgencia) {
        return urgencia === "critica"
            ? '<span class="insignia urgencia-critica">' + A.iconos.exclamacion + '<span class="visualmente-oculto">Urgencia: </span>Crítica</span>'
            : '<span class="insignia urgencia-normal">' + A.iconos.guion + '<span class="visualmente-oculto">Urgencia: </span>Normal</span>';
    }

    // Chip "Recurso · estado" para las tarjetas.
    function chipRecurso(r) {
        const e = estados[r.estado];
        return '<span class="insignia sol-' + r.estado + '">' + e.icono + A.escapar(A.nombreRecurso(r.tipo_recurso)) +
            ' <span aria-hidden="true">·</span><span class="visualmente-oculto">:</span> ' + e.texto + "</span>";
    }

    const plural = (n, uno, varios) => n + " " + (n === 1 ? uno : varios);

    function tarjetaSolicitud(s, opciones) {
        opciones = opciones || {};
        const nivel = opciones.nivelTitulo || "h3";
        return '<article class="tarjeta solicitud" aria-labelledby="titulo-' + s.id + '">' +
            '<div class="cabeza">' +
                "<" + nivel + ' id="titulo-' + s.id + '"><a href="solicitud.html?id=' + s.id + '">Solicitud #' + s.numero + "</a></" + nivel + ">" +
                '<div class="insignias">' + insigniaSolicitud(s.estado) + (s.urgencia === "critica" ? insigniaUrgencia("critica") : "") + "</div>" +
            "</div>" +
            '<p class="meta">' + A.escapar(s.zona_nombre) + " · " + plural(s.personas_afectadas, "persona afectada", "personas afectadas") +
                " · creada <time datetime=\"" + s.creado_at + '" title="' + A.fechaCompleta(s.creado_at) + '">' + A.haceCuanto(s.creado_at) + "</time></p>" +
            (s.descripcion ? '<p class="descripcion">' + A.escapar(s.descripcion) + "</p>" : "") +
            '<ul class="lista-recursos" aria-label="Recursos pedidos">' + s.recursos.map((r) => "<li>" + chipRecurso(r) + "</li>").join("") + "</ul>" +
            '<div class="pie-solicitud"><span>Actualizada ' + A.haceCuanto(s.actualizado_at) + "</span>" +
                '<a href="solicitud.html?id=' + s.id + '">Ver detalle<span class="visualmente-oculto"> de la solicitud ' + s.numero + "</span></a></div>" +
        "</article>";
    }

    // Quién atiende un recurso, en texto.
    function textoAsignado(a) {
        if (!a) return "Sin asignar";
        return A.escapar(a.nombre) + (a.tipo === "organizacion" ? " (organización)" : " (voluntario/a)");
    }

    window.AyudaYaUsuario = { iconos, estados, abiertas, esAbierta, insigniaSolicitud, insigniaUrgencia, chipRecurso, tarjetaSolicitud, textoAsignado, plural, usuario: DU.usuario };
})();

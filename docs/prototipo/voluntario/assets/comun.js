// Funciones compartidas por todas las pantallas del prototipo.
(function () {
    "use strict";

    document.documentElement.classList.add("js");

    const iconos = {
        barras3: '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false"><rect x="1" y="9" width="4" height="6" rx="1" fill="currentColor"/><rect x="6" y="5" width="4" height="10" rx="1" fill="currentColor"/><rect x="11" y="1" width="4" height="14" rx="1" fill="currentColor"/></svg>',
        barras2: '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false"><rect x="1" y="9" width="4" height="6" rx="1" fill="currentColor"/><rect x="6" y="5" width="4" height="10" rx="1" fill="currentColor"/><rect x="11.5" y="1.5" width="3" height="13" rx="1" fill="none" stroke="currentColor"/></svg>',
        barras1: '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false"><rect x="1" y="9" width="4" height="6" rx="1" fill="currentColor"/><rect x="6.5" y="5.5" width="3" height="9" rx="1" fill="none" stroke="currentColor"/><rect x="11.5" y="1.5" width="3" height="13" rx="1" fill="none" stroke="currentColor"/></svg>',
        cruz: '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/></svg>',
        guion: '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="M4 8h8" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>',
        check: '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="M3 8.5l3.2 3L13 4.5" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        exclamacion: '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="M8 1.5L15 14H1z" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/><path d="M8 6v3.5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/><circle cx="8" cy="11.8" r="1" fill="currentColor"/></svg>',
        pausa: '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false"><circle cx="8" cy="8" r="6.5" fill="none" stroke="currentColor" stroke-width="1.6"/><path d="M6.3 5.5v5M9.7 5.5v5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>',
        alerta: '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M12 2.5L22.5 21h-21z" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/><path d="M12 9v5" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/><circle cx="12" cy="17.5" r="1.3" fill="currentColor"/></svg>',
        info: '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><circle cx="12" cy="12" r="10" fill="none" stroke="currentColor" stroke-width="2"/><path d="M12 11v6" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/><circle cx="12" cy="7.5" r="1.3" fill="currentColor"/></svg>',
        ok: '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><circle cx="12" cy="12" r="10" fill="none" stroke="currentColor" stroke-width="2"/><path d="M7 12.5l3.3 3.2L17 9" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/></svg>'
    };

    // Niveles de centro_recurso.nivel. El ícono cambia de forma, no solo de color.
    const niveles = {
        alta: { texto: "Alta", icono: iconos.barras3, descripcion: "hay suficiente" },
        media: { texto: "Media", icono: iconos.barras2, descripcion: "queda una cantidad moderada" },
        baja: { texto: "Baja", icono: iconos.barras1, descripcion: "queda poco, conviene llamar antes" },
        sin_stock: { texto: "Sin stock", icono: iconos.cruz, descripcion: "lo ofrece, pero hoy no tiene" },
        no_ofrece: { texto: "No ofrece", icono: iconos.guion, descripcion: "este centro no trabaja con ese recurso" }
    };

    // Valores de centro.estado_operativo.
    const estados = {
        operativo: { texto: "Abierto", icono: iconos.check },
        saturado: { texto: "Saturado", icono: iconos.exclamacion },
        cerrado: { texto: "Cerrado temporalmente", icono: iconos.pausa }
    };

    const tiposOrganizacion = { comunitaria: "Organización comunitaria", institucional: "Organización institucional" };

    function escapar(texto) {
        return String(texto ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
    }

    function insigniaNivel(nivel, nombreRecurso, extraClase) {
        const n = niveles[nivel];
        const clase = "insignia nivel-" + nivel + (extraClase ? " " + extraClase : "");
        const contenido = n.icono + (nombreRecurso
            ? escapar(nombreRecurso) + ' <span aria-hidden="true">·</span><span class="visualmente-oculto">: disponibilidad</span> ' + n.texto
            : n.texto);
        return '<span class="' + clase + '">' + contenido + "</span>";
    }

    function insigniaEstado(estado) {
        const e = estados[estado];
        return '<span class="insignia estado-' + estado + '">' + e.icono + '<span class="visualmente-oculto">Estado: </span>' + e.texto + "</span>";
    }

    const formatoRelativo = new Intl.RelativeTimeFormat("es-AR", { numeric: "auto" });
    const formatoFecha = new Intl.DateTimeFormat("es-AR", { dateStyle: "long", timeStyle: "short" });

    function haceCuanto(iso) {
        const minutos = Math.round((new Date(iso).getTime() - Date.now()) / 60000);
        if (Math.abs(minutos) < 60) return formatoRelativo.format(Math.min(minutos, -1), "minute");
        const horas = Math.round(minutos / 60);
        if (Math.abs(horas) < 24) return formatoRelativo.format(horas, "hour");
        return formatoRelativo.format(Math.round(horas / 24), "day");
    }

    function fechaCompleta(iso) {
        return formatoFecha.format(new Date(iso));
    }

    function horasDesde(iso) {
        return (Date.now() - new Date(iso).getTime()) / 3600000;
    }

    function nombreRecurso(codigo) {
        return window.DATOS.tiposRecurso.find((t) => t.codigo === codigo).nombre;
    }

    // Recursos que ofrece un centro, en el orden del catálogo.
    function recursosDe(centro) {
        return window.DATOS.tiposRecurso
            .filter((t) => centro.recursos[t.codigo])
            .map((t) => ({ codigo: t.codigo, nombre: t.nombre, nivel: centro.recursos[t.codigo] }));
    }

    function tarjetaCentro(c, opciones) {
        opciones = opciones || {};
        const nivel = (opciones.nivelTitulo || "h3");
        const recursos = recursosDe(c).map((r) =>
            "<li>" + insigniaNivel(r.nivel, r.nombre, r.codigo === opciones.recursoResaltado ? "recurso-filtrado" : "") + "</li>"
        ).join("");
        const verEnMapa = opciones.botonMapa && c.latitud !== null
            ? '<button type="button" class="boton-texto" data-ver-mapa="' + c.id + '">Ver en el mapa<span class="visualmente-oculto"> ' + escapar(c.nombre) + "</span></button>"
            : (opciones.botonMapa ? "<span>Sin ubicación en el mapa</span>" : "");

        return '<article class="tarjeta centro" id="centro-' + c.id + '" aria-labelledby="titulo-' + c.id + '">' +
            "<" + nivel + ' id="titulo-' + c.id + '"><a href="centro.html?id=' + c.id + '">' + escapar(c.nombre) + "</a></" + nivel + ">" +
            '<p class="meta">' + escapar(c.tipo_centro_nombre) + " · " + escapar(c.zona_nombre) + "</p>" +
            '<p class="estado">' + insigniaEstado(c.estado_operativo) + "</p>" +
            '<ul class="lista-recursos" aria-label="Recursos que ofrece">' + recursos + "</ul>" +
            '<div class="pie-centro"><span>Disponibilidad actualizada ' +
            '<time datetime="' + c.recursos_actualizado_at + '" title="' + fechaCompleta(c.recursos_actualizado_at) + '">' + haceCuanto(c.recursos_actualizado_at) + "</time></span>" +
            verEnMapa + "</div>" +
            "</article>";
    }

    // Capa base del mapa. Los mosaicos de OpenStreetMap exigen el encabezado
    // Referer y, al abrir el prototipo con doble clic (file://), Chrome no lo
    // manda: OSM devuelve un mosaico "403 Access blocked". En ese caso se usa
    // el estilo humanitario (HOT) que sirve OSM Francia con los mismos datos.
    function capaMapa() {
        if (location.protocol === "file:") {
            return L.tileLayer("https://{s}.tile.openstreetmap.fr/hot/{z}/{x}/{y}.png", {
                subdomains: "abc",
                maxZoom: 19,
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> · <a href="https://www.hotosm.org/">HOT</a> / <a href="https://openstreetmap.fr/">OSM France</a>'
            });
        }
        return L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        });
    }

    // Menú en pantallas chicas.
    function iniciarMenu() {
        const boton = document.querySelector(".boton-menu");
        const nav = document.getElementById("nav-principal");
        if (!boton || !nav) return;

        boton.addEventListener("click", () => {
            const abierto = boton.getAttribute("aria-expanded") === "true";
            boton.setAttribute("aria-expanded", String(!abierto));
            nav.classList.toggle("abierto", !abierto);
        });

        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && nav.classList.contains("abierto")) {
                boton.setAttribute("aria-expanded", "false");
                nav.classList.remove("abierto");
                boton.focus();
            }
        });
    }

    document.addEventListener("DOMContentLoaded", iniciarMenu);

    window.AyudaYa = {
        iconos, niveles, estados, tiposOrganizacion,
        escapar, insigniaNivel, insigniaEstado, haceCuanto, fechaCompleta, horasDesde,
        nombreRecurso, recursosDe, tarjetaCentro, capaMapa
    };
})();

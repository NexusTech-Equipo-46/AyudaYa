// Pantalla /centros/{id}: detalle público de un centro.
(function () {
    "use strict";

    const A = window.AyudaYa;
    const { centros, tiposRecurso } = window.DATOS;
    const contenedor = document.getElementById("detalle-centro");

    const id = new URLSearchParams(location.search).get("id");
    const c = centros.find((x) => x.id === id);

    if (!c) {
        document.title = "Centro no encontrado · AyudaYa";
        contenedor.innerHTML =
            '<div class="no-encontrado">' +
            "<h1>No encontramos ese centro</h1>" +
            "<p>Puede que ya no esté publicado o que el enlace esté incompleto.</p>" +
            '<a class="boton" href="mapa.html">Ver todos los centros</a>' +
            "</div>";
        return;
    }

    document.title = c.nombre + " · AyudaYa";

    const horas = A.horasDesde(c.recursos_actualizado_at);
    const avisos = [];

    if (c.estado_operativo === "cerrado") {
        avisos.push('<div class="alerta alerta-peligro">' + A.iconos.alerta +
            "<p><strong>Este centro está cerrado temporalmente.</strong> Buscá otro centro abierto en el mapa.</p></div>");
    } else if (c.estado_operativo === "saturado") {
        avisos.push('<div class="alerta alerta-advertencia">' + A.iconos.alerta +
            "<p><strong>Este centro está saturado.</strong> Puede que no pueda recibir a más personas. Llamá antes de ir.</p></div>");
    }

    if (horas > 24) {
        avisos.push('<div class="alerta alerta-advertencia">' + A.iconos.alerta +
            "<p><strong>La disponibilidad se actualizó " + A.haceCuanto(c.recursos_actualizado_at) + ".</strong> " +
            "Puede no estar al día: confirmá por teléfono antes de ir.</p></div>");
    }

    // Primero lo que ofrece (en orden de catálogo) y al final lo que no.
    const ordenados = tiposRecurso.filter((t) => c.recursos[t.codigo])
        .concat(tiposRecurso.filter((t) => !c.recursos[t.codigo]));

    const filas = ordenados.map((t) => {
        const nivel = c.recursos[t.codigo] || "no_ofrece";
        return "<tr><th scope=\"row\">" + A.escapar(t.nombre) + "</th><td>" + A.insigniaNivel(nivel) + "</td></tr>";
    }).join("");

    const referencias = ["alta", "media", "baja", "sin_stock"].map((n) =>
        "<li>" + A.insigniaNivel(n) + " " + A.niveles[n].descripcion + "</li>"
    ).join("");

    const tel = c.telefono_contacto.replace(/\D/g, "");
    const comoLlegar = c.latitud !== null
        ? "https://www.google.com/maps/dir/?api=1&destination=" + c.latitud + "," + c.longitud
        : "https://www.google.com/maps/search/?api=1&query=" + encodeURIComponent(c.direccion);

    contenedor.innerHTML =
        '<nav class="migas" aria-label="Estás en">' +
            "<ol>" +
                '<li><a href="index.html">Inicio</a></li>' +
                '<li><a href="mapa.html">Mapa de centros</a></li>' +
                '<li><a href="centro.html?id=' + c.id + '" aria-current="page">' + A.escapar(c.nombre) + "</a></li>" +
            "</ol>" +
        "</nav>" +

        '<div class="detalle-encabezado">' +
            "<div>" +
                "<h1>" + A.escapar(c.nombre) + "</h1>" +
                '<p class="meta">' + A.escapar(c.tipo_centro_nombre) + " · " + A.escapar(c.zona_nombre) + ", " + A.escapar(c.ciudad_nombre) + "</p>" +
                '<p class="meta">' + A.escapar(c.organizacion_nombre) + " (" + A.tiposOrganizacion[c.organizacion_tipo].toLowerCase() + ")</p>" +
            "</div>" +
            A.insigniaEstado(c.estado_operativo) +
        "</div>" +

        '<div class="detalle">' +
            "<div>" +
                avisos.join("") +

                '<section class="tarjeta" aria-labelledby="titulo-recursos">' +
                    '<h2 id="titulo-recursos">Disponibilidad de recursos</h2>' +
                    '<table class="tabla-recursos">' +
                        "<caption>Actualizada " +
                            '<time datetime="' + c.recursos_actualizado_at + '">' + A.haceCuanto(c.recursos_actualizado_at) + "</time>" +
                            " (" + A.fechaCompleta(c.recursos_actualizado_at) + ")" +
                        "</caption>" +
                        '<thead><tr><th scope="col">Recurso</th><th scope="col">Disponibilidad</th></tr></thead>' +
                        "<tbody>" + filas + "</tbody>" +
                    "</table>" +
                    '<h3 class="visualmente-oculto">Qué significa cada nivel</h3>' +
                    '<ul class="referencias">' + referencias + "</ul>" +
                "</section>" +

                (c.descripcion
                    ? '<section class="tarjeta" aria-labelledby="titulo-descripcion"><h2 id="titulo-descripcion">Sobre este centro</h2><p>' + A.escapar(c.descripcion) + "</p></section>"
                    : "") +
            "</div>" +

            "<div>" +
                '<section class="tarjeta" aria-labelledby="titulo-contacto">' +
                    '<h2 id="titulo-contacto">Contacto y ubicación</h2>' +
                    '<dl class="datos-contacto">' +
                        "<div><dt>Dirección</dt><dd>" + A.escapar(c.direccion) + "</dd></div>" +
                        '<div><dt>Teléfono</dt><dd><a href="tel:' + tel + '">' + A.escapar(c.telefono_contacto) + "</a></dd></div>" +
                        (c.email_contacto
                            ? '<div><dt>Correo</dt><dd><a href="mailto:' + A.escapar(c.email_contacto) + '">' + A.escapar(c.email_contacto) + "</a></dd></div>"
                            : "") +
                    "</dl>" +
                    (c.latitud !== null && window.L
                        ? '<div id="mapa-centro" role="region" aria-label="Mapa con la ubicación de ' + A.escapar(c.nombre) + '"></div>'
                        : '<p class="texto-suave" style="margin-top:1rem">' +
                            (c.latitud === null ? "Este centro no cargó su ubicación en el mapa." : "No se pudo cargar el mapa. Usá la dirección o “Cómo llegar”.") +
                          "</p>") +
                    '<a class="boton boton-secundario" href="' + comoLlegar + '" target="_blank" rel="noopener">Cómo llegar<span class="visualmente-oculto"> (se abre en otra pestaña)</span></a>' +
                "</section>" +

                '<section class="tarjeta llamado" aria-labelledby="titulo-llamado">' +
                    '<h2 id="titulo-llamado">¿No encontrás lo que necesitás?</h2>' +
                    "<p>Con una cuenta podés pedir ayuda y un voluntario u organización de tu zona va a poder asistirte.</p>" +
                    '<div class="acciones">' +
                        '<a class="boton" href="login.html?volver=nueva-solicitud">Pedir ayuda</a>' +
                        '<a class="boton boton-secundario" href="registro.html?para=voluntario">Quiero ser voluntario</a>' +
                    "</div>" +
                "</section>" +
            "</div>" +
        "</div>";

    if (c.latitud !== null && window.L) {
        const mapa = L.map("mapa-centro", { scrollWheelZoom: false, keyboard: false }).setView([c.latitud, c.longitud], 15);
        A.capaMapa().addTo(mapa);
        L.marker([c.latitud, c.longitud], { title: c.nombre, alt: c.nombre, keyboard: false }).addTo(mapa);
    }
})();

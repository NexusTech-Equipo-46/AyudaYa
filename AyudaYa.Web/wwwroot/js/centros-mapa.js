// Centros públicos: filtros, lista y mapa usan los datos recibidos del servidor.
(function () {
    "use strict";

    const datosElemento = document.getElementById("datos-centros");
    const { centros, tiposRecurso } = JSON.parse(datosElemento.textContent);
    const urlDetalle = datosElemento.dataset.urlDetalle;
    const zonas = [...new Map(centros.filter((c) => c.zona_id && c.zona_nombre)
        .map((c) => [c.zona_id, { id: c.zona_id, nombre: c.zona_nombre + (c.ciudad_nombre ? " · " + c.ciudad_nombre : "") }])).values()];
    const tiposCentro = [...new Map(centros.filter((c) => c.tipo_centro_codigo && c.tipo_centro_nombre)
        .map((c) => [c.tipo_centro_codigo, { codigo: c.tipo_centro_codigo, nombre: c.tipo_centro_nombre }])).values()];
    // Las funciones compartidas consultan este catálogo, no la lista ficticia.
    window.DATOS = { tiposRecurso };
    const A = window.AyudaYa;

    const form = document.getElementById("filtros");
    const campos = {
        q: document.getElementById("f-buscar"),
        zona: document.getElementById("f-zona"),
        tipo: document.getElementById("f-tipo"),
        recurso: document.getElementById("f-recurso"),
        abiertos: document.getElementById("f-abiertos")
    };
    const lista = document.getElementById("lista-centros");
    const sinResultados = document.getElementById("sin-resultados");
    const cantidad = document.getElementById("cantidad");
    const filtrosActivos = document.getElementById("filtros-activos");
    const resultados = document.getElementById("resultados");
    const panelFiltros = document.getElementById("panel-filtros");

    const ciudades = [...new Set(centros.map((c) => c.ciudad_nombre).filter(Boolean))];
    document.getElementById("nombre-ciudad").textContent = ciudades.length === 1 ? " en " + ciudades[0] : "";

    function coordenadasValidas(c) {
        return typeof c.latitud === "number" && Number.isFinite(c.latitud) && c.latitud >= -90 && c.latitud <= 90 &&
            typeof c.longitud === "number" && Number.isFinite(c.longitud) && c.longitud >= -180 && c.longitud <= 180;
    }

    function enlaceDetalle(id) {
        return urlDetalle + "?id=" + encodeURIComponent(id);
    }

    function insigniaEstado(estado) {
        return A.estados[estado] ? A.insigniaEstado(estado) : '<span class="insignia">Estado no informado</span>';
    }

    function tarjetaCentro(c, recurso, hayMapa) {
        const niveles = c.recursos || {};
        const recursos = Object.entries(niveles).map(([codigo, nivel]) => {
            const tipo = tiposRecurso.find((t) => t.codigo === codigo);
            const nombre = tipo ? tipo.nombre : codigo.replaceAll("_", " ");
            return "<li>" + (A.niveles[nivel] ? A.insigniaNivel(nivel, nombre, codigo === recurso ? "recurso-filtrado" : "")
                : '<span class="insignia">' + A.escapar(nombre) + ' · Sin informar</span>') + "</li>";
        }).join("");
        const fecha = c.recursos_actualizado_at && !Number.isNaN(new Date(c.recursos_actualizado_at).getTime())
            ? 'Disponibilidad actualizada <time datetime="' + A.escapar(c.recursos_actualizado_at) + '" title="' + A.escapar(A.fechaCompleta(c.recursos_actualizado_at)) + '">' + A.escapar(A.haceCuanto(c.recursos_actualizado_at)) + "</time>"
            : "Fecha de actualización no informada";
        const verEnMapa = hayMapa && coordenadasValidas(c)
            ? '<button type="button" class="boton-texto" data-ver-mapa="' + A.escapar(c.id) + '">Ver en el mapa<span class="visualmente-oculto"> ' + A.escapar(c.nombre) + "</span></button>"
            : (!coordenadasValidas(c) ? "<span>Sin ubicación en el mapa</span>" : "");
        return '<article class="tarjeta centro" id="centro-' + A.escapar(c.id) + '" aria-labelledby="titulo-' + A.escapar(c.id) + '">' +
            '<h3 id="titulo-' + A.escapar(c.id) + '"><a href="' + A.escapar(enlaceDetalle(c.id)) + '">' + A.escapar(c.nombre) + "</a></h3>" +
            '<p class="meta">' + A.escapar(c.tipo_centro_nombre || "Tipo no informado") + " · " + A.escapar(c.zona_nombre || c.ciudad_nombre || "Zona no informada") + "</p>" +
            '<p class="estado">' + insigniaEstado(c.estado_operativo) + "</p>" +
            '<ul class="lista-recursos" aria-label="Recursos que ofrece">' + recursos + "</ul>" +
            '<div class="pie-centro"><span>' + fecha + "</span>" + verEnMapa + "</div></article>";
    }

    // --- Opciones de los selects, desde los catálogos ---
    const opcion = (valor, texto) => {
        const o = document.createElement("option");
        o.value = valor;
        o.textContent = texto;
        return o;
    };
    zonas.forEach((z) => campos.zona.append(opcion(z.id, z.nombre)));
    tiposCentro.forEach((t) => campos.tipo.append(opcion(t.codigo, t.nombre)));
    tiposRecurso.forEach((t) => campos.recurso.append(opcion(t.codigo, t.nombre)));

    // --- Filtros iniciales desde la URL (ej: mapa.html?recurso=agua) ---
    const params = new URLSearchParams(location.search);
    campos.q.value = params.get("q") || "";
    let filtroInvalido = false;
    ["zona", "tipo", "recurso"].forEach((k) => {
        const v = params.get(k);
        if (v && [...campos[k].options].some((o) => o.value === v)) campos[k].value = v;
        else if (v) filtroInvalido = true;
    });
    document.getElementById("filtro-invalido").hidden = !filtroInvalido;
    campos.abiertos.checked = params.get("abiertos") === "1";

    // En celular los filtros arrancan cerrados para que se vean los resultados.
    const esChico = window.matchMedia("(max-width: 760px)");
    if (esChico.matches) panelFiltros.open = false;

    // --- Mapa (si Leaflet no cargó, la lista sigue funcionando) ---
    let mapa = null;
    const marcadores = new Map();

    if (window.L) {
        try {
        mapa = L.map("mapa", { scrollWheelZoom: false }).setView([-38.4161, -63.6167], 4);
        const capa = A.capaMapa();
        capa.on("tileerror", () => { document.getElementById("mapa-error").hidden = false; });
        capa.addTo(mapa);

        centros.filter(coordenadasValidas).forEach((c) => {
            const m = L.marker([c.latitud, c.longitud], { title: c.nombre, alt: c.nombre, keyboard: true });
            m.bindPopup(
                "<h3>" + A.escapar(c.nombre) + "</h3>" +
                "<p>" + A.escapar(c.tipo_centro_nombre) + " · " + A.escapar(c.zona_nombre) + "</p>" +
                "<p>" + insigniaEstado(c.estado_operativo) + "</p>" +
                '<a class="boton" href="' + A.escapar(enlaceDetalle(c.id)) + '">Ver detalle</a>'
            );
            m.on("click", () => destacar(c.id));
            marcadores.set(c.id, m);
        });
        } catch (_) {
            if (mapa) mapa.remove();
            mapa = null;
            document.getElementById("mapa-error").hidden = false;
            document.getElementById("nota-mapa").hidden = true;
        }
    } else {
        document.getElementById("mapa-error").hidden = false;
        document.getElementById("nota-mapa").hidden = true;
    }

    function destacar(id) {
        lista.querySelectorAll(".centro.destacado").forEach((el) => el.classList.remove("destacado"));
        const tarjeta = document.getElementById("centro-" + id);
        if (tarjeta && !esChico.matches) {
            tarjeta.classList.add("destacado");
            tarjeta.scrollIntoView({ block: "nearest" });
        }
    }

    // --- Filtrado y orden ---
    const pesoNivel = { alta: 0, media: 1, baja: 2 };
    const pesoEstado = { operativo: 0, saturado: 1, cerrado: 2 };
    const normalizar = (s) => String(s || "").normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();

    function filtrar() {
        const q = normalizar(campos.q.value.trim());
        const recurso = campos.recurso.value;

        return centros
            .filter((c) => !q || normalizar(c.nombre + " " + c.direccion).includes(q))
            .filter((c) => !campos.zona.value || c.zona_id === campos.zona.value)
            .filter((c) => !campos.tipo.value || c.tipo_centro_codigo === campos.tipo.value)
            .filter((c) => !recurso || ["alta", "media", "baja"].includes((c.recursos || {})[recurso]))
            .filter((c) => !campos.abiertos.checked || c.estado_operativo !== "cerrado")
            .sort((a, b) =>
                (recurso ? pesoNivel[a.recursos[recurso]] - pesoNivel[b.recursos[recurso]] : 0) ||
                (pesoEstado[a.estado_operativo] ?? 3) - (pesoEstado[b.estado_operativo] ?? 3) ||
                (Date.parse(b.recursos_actualizado_at) || 0) - (Date.parse(a.recursos_actualizado_at) || 0)
            );
    }

    function contarFiltrosActivos() {
        return ["q", "zona", "tipo", "recurso"].filter((k) => campos[k].value.trim()).length + (campos.abiertos.checked ? 1 : 0);
    }

    function actualizarUrl() {
        const p = new URLSearchParams();
        ["q", "zona", "tipo", "recurso"].forEach((k) => { if (campos[k].value.trim()) p.set(k, campos[k].value.trim()); });
        if (campos.abiertos.checked) p.set("abiertos", "1");
        const qs = p.toString();
        history.replaceState(null, "", qs ? "?" + qs : location.pathname);
    }

    function render() {
        const encontrados = filtrar();
        const recurso = campos.recurso.value;

        lista.innerHTML = encontrados
            .map((c) => "<li>" + tarjetaCentro(c, recurso, !!mapa) + "</li>")
            .join("");
        sinResultados.hidden = encontrados.length > 0;
        document.getElementById("titulo-sin-resultados").textContent = centros.length ? "No hay centros con esos filtros" : "Todavía no hay centros para mostrar";
        document.getElementById("texto-sin-resultados").textContent = centros.length ? "Probá con otra zona o quitá algún filtro." : "Cuando haya centros públicos disponibles, aparecerán acá.";
        document.getElementById("limpiar-vacio").hidden = centros.length === 0;

        const sinUbicacion = encontrados.filter((c) => !coordenadasValidas(c)).length;
        cantidad.textContent =
            (encontrados.length === 1 ? "1 centro encontrado" : encontrados.length + " centros encontrados") +
            (recurso ? " con " + A.nombreRecurso(recurso).toLowerCase() + " disponible" : "") +
            (sinUbicacion ? " (" + sinUbicacion + " sin ubicación en el mapa)" : "");

        const activos = contarFiltrosActivos();
        filtrosActivos.textContent = activos ? "(" + activos + (activos === 1 ? " activo)" : " activos)") : "";

        if (mapa) {
            const visibles = new Set(encontrados.map((c) => c.id));
            puntosVisibles = [];
            marcadores.forEach((m, id) => {
                if (visibles.has(id)) {
                    m.addTo(mapa);
                    puntosVisibles.push(m.getLatLng());
                } else {
                    m.remove();
                }
            });
            encuadrar();
        }

        actualizarUrl();
    }

    // Si el mapa está oculto (vista Lista en celular) mide 0x0 y el encuadre
    // sale mal: se hace al mostrarlo, en cambiarVista.
    let puntosVisibles = [];
    function encuadrar() {
        if (!mapa || !puntosVisibles.length || !mapa.getContainer().offsetWidth) return;
        mapa.fitBounds(L.latLngBounds(puntosVisibles).pad(0.25), { maxZoom: 15 });
    }

    // --- Vista lista / mapa (solo en pantallas chicas) ---
    const botonesVista = document.querySelectorAll(".selector-vista button");

    function cambiarVista(vista) {
        resultados.dataset.vista = vista;
        botonesVista.forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.vista === vista)));
        if (vista === "mapa" && mapa) {
            mapa.invalidateSize();
            encuadrar();
        }
    }

    botonesVista.forEach((b) => b.addEventListener("click", () => cambiarVista(b.dataset.vista)));
    if (params.get("vista") === "mapa") cambiarVista("mapa");

    // "Ver en el mapa" desde una tarjeta
    lista.addEventListener("click", (e) => {
        const boton = e.target.closest("[data-ver-mapa]");
        if (!boton) return;
        const m = marcadores.get(boton.dataset.verMapa);
        if (!m) return;
        if (esChico.matches) cambiarVista("mapa");
        destacar(boton.dataset.verMapa);
        mapa.setView(m.getLatLng(), 15);
        m.openPopup();
        const el = m.getElement();
        if (el) el.focus();
    });

    // --- Eventos de filtros ---
    form.addEventListener("submit", (e) => e.preventDefault());
    form.addEventListener("input", render);
    form.addEventListener("change", render);
    form.addEventListener("reset", () => setTimeout(render, 0));
    document.getElementById("limpiar-vacio").addEventListener("click", () => {
        form.reset();
        campos.q.focus();
    });

    render();
})();

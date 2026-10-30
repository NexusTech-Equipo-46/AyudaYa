// Pantalla /mapa: filtros, lista y mapa de centros sincronizados.
(function () {
    "use strict";

    const { ciudad, zonas, tiposCentro, tiposRecurso, centros } = window.DATOS;
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

    document.getElementById("nombre-ciudad").textContent = ciudad.nombre;

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
    ["zona", "tipo", "recurso"].forEach((k) => {
        const v = params.get(k);
        if (v && [...campos[k].options].some((o) => o.value === v)) campos[k].value = v;
    });
    campos.abiertos.checked = params.get("abiertos") === "1";

    // En celular los filtros arrancan cerrados para que se vean los resultados.
    const esChico = window.matchMedia("(max-width: 760px)");
    if (esChico.matches) panelFiltros.open = false;

    // --- Mapa (si Leaflet no cargó, la lista sigue funcionando) ---
    let mapa = null;
    const marcadores = new Map();

    if (window.L) {
        mapa = L.map("mapa", { scrollWheelZoom: false }).setView([-34.455, -58.64], 12);
        A.capaMapa().addTo(mapa);

        centros.filter((c) => c.latitud !== null).forEach((c) => {
            const m = L.marker([c.latitud, c.longitud], { title: c.nombre, alt: c.nombre, keyboard: true });
            m.bindPopup(
                "<h3>" + A.escapar(c.nombre) + "</h3>" +
                "<p>" + A.escapar(c.tipo_centro_nombre) + " · " + A.escapar(c.zona_nombre) + "</p>" +
                "<p>" + A.insigniaEstado(c.estado_operativo) + "</p>" +
                '<a class="boton" href="centro.html?id=' + c.id + '">Ver detalle</a>'
            );
            m.on("click", () => destacar(c.id));
            marcadores.set(c.id, m);
        });
    } else {
        document.getElementById("mapa").innerHTML =
            '<div class="alerta alerta-advertencia" style="margin:1rem">' + A.iconos.alerta +
            "<p>No se pudo cargar el mapa. La lista tiene la misma información.</p></div>";
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
    const normalizar = (s) => s.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();

    function filtrar() {
        const q = normalizar(campos.q.value.trim());
        const recurso = campos.recurso.value;

        return centros
            .filter((c) => !q || normalizar(c.nombre + " " + c.direccion).includes(q))
            .filter((c) => !campos.zona.value || c.zona_id === campos.zona.value)
            .filter((c) => !campos.tipo.value || c.tipo_centro_codigo === campos.tipo.value)
            .filter((c) => !recurso || (c.recursos[recurso] && c.recursos[recurso] !== "sin_stock"))
            .filter((c) => !campos.abiertos.checked || c.estado_operativo !== "cerrado")
            .sort((a, b) =>
                (recurso ? pesoNivel[a.recursos[recurso]] - pesoNivel[b.recursos[recurso]] : 0) ||
                pesoEstado[a.estado_operativo] - pesoEstado[b.estado_operativo] ||
                new Date(b.recursos_actualizado_at) - new Date(a.recursos_actualizado_at)
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
            .map((c) => "<li>" + A.tarjetaCentro(c, { botonMapa: !!mapa, recursoResaltado: recurso }) + "</li>")
            .join("");
        sinResultados.hidden = encontrados.length > 0;

        const sinUbicacion = encontrados.filter((c) => c.latitud === null).length;
        cantidad.textContent =
            (encontrados.length === 1 ? "1 centro encontrado" : encontrados.length + " centros encontrados") +
            (recurso ? " con " + A.nombreRecurso(recurso).toLowerCase() + " disponible" : "") +
            (sinUbicacion && mapa ? " (" + sinUbicacion + " sin ubicación en el mapa)" : "");

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

// Mapa público de centros: filtros, lista y mapa sincronizados.
// La lista se renderiza en el servidor; acá solo se filtra, se ordena y se
// muestran/ocultan las tarjetas y los marcadores.
(function () {
    "use strict";

    const leer = (id) => {
        const el = document.getElementById(id);
        try { return el ? JSON.parse(el.textContent) : []; } catch (_) { return []; }
    };
    const centros = leer("datos-centros");
    const necesidades = leer("datos-necesidades");

    const form = document.getElementById("filtros");
    const campos = {
        q: document.getElementById("f-buscar"),
        zona: document.getElementById("f-zona"),
        tipo: document.getElementById("f-tipo"),
        recurso: document.getElementById("f-recurso"),
        abiertos: document.getElementById("f-abiertos"),
        necesidades: document.getElementById("f-necesidades")
    };
    const lista = document.getElementById("lista-centros");
    const sinResultados = document.getElementById("sin-resultados");
    const cantidad = document.getElementById("cantidad");
    const filtrosActivos = document.getElementById("filtros-activos");
    const resultados = document.getElementById("resultados");
    const panelFiltros = document.getElementById("panel-filtros");
    const filasNecesidades = document.querySelectorAll("#tabla-pedidos-zona tr[data-zona]");
    const tablaNecesidades = document.getElementById("tabla-pedidos-zona");
    const sinPedidosZona = document.getElementById("sin-pedidos-zona");
    const nombreRecurso = (codigo) => {
        const opcion = campos.recurso && [...campos.recurso.options].find((o) => o.value === codigo);
        return opcion ? opcion.textContent : codigo;
    };

    // --- Filtros iniciales desde la URL ---
    const params = new URLSearchParams(location.search);
    campos.q.value = params.get("q") || "";
    ["zona", "tipo", "recurso"].forEach((k) => {
        const v = params.get(k);
        if (v && [...campos[k].options].some((o) => o.value === v)) campos[k].value = v;
    });
    campos.abiertos.checked = params.get("abiertos") === "1";
    if (campos.necesidades) campos.necesidades.checked = params.get("necesidades") === "1";

    const esChico = window.matchMedia("(max-width: 760px)");
    if (esChico.matches) panelFiltros.open = false;

    // --- Mapa (si Leaflet no cargó, la lista sigue funcionando) ---
    let mapa = null;
    const marcadores = new Map();
    let capaNecesidades = null;
    const circulosNecesidades = [];

    function popupCentro(c) {
        const caja = document.createElement("div");
        const titulo = document.createElement("h3");
        titulo.textContent = c.nombre;
        const tipo = document.createElement("p");
        tipo.textContent = c.tipoNombre + " · " + c.zona;
        const enlace = document.createElement("a");
        enlace.className = "boton";
        enlace.href = "/Centros/Detalle/" + encodeURIComponent(c.id);
        enlace.textContent = "Ver detalle";
        caja.append(titulo, tipo, enlace);
        return caja;
    }

    if (window.L) {
        mapa = L.map("mapa", { scrollWheelZoom: false }).setView([-34.6, -58.4], 11);
        MapaAyudaYa.capaBase().addTo(mapa);

        centros.filter((c) => c.latitud !== null && c.longitud !== null).forEach((c) => {
            const m = L.marker([c.latitud, c.longitud], { title: c.nombre, alt: c.nombre, keyboard: true });
            m.bindPopup(popupCentro(c));
            m.on("click", () => destacar(c.id));
            marcadores.set(c.id, m);
        });

        capaNecesidades = L.layerGroup();
        necesidades.forEach((n) => {
            const circulo = L.circle([n.latitud, n.longitud], {
                radius: 350 + Math.min(n.personas, 60) * 25,
                color: "#B45309", weight: 2, fillColor: "#B45309", fillOpacity: 0.18
            });
            const caja = document.createElement("div");
            const t = document.createElement("h3");
            t.textContent = n.zona;
            const p = document.createElement("p");
            p.textContent = n.solicitudes + (n.solicitudes === 1 ? " pedido abierto" : " pedidos abiertos") +
                " · " + n.personas + (n.personas === 1 ? " persona" : " personas") + " (ubicación aproximada)";
            const r = document.createElement("p");
            r.textContent = n.recursos.map((x) => x.nombre).join(", ");
            caja.append(t, p, r);
            circulo.bindPopup(caja);
            circulosNecesidades.push({ zonaId: n.zonaId, circulo });
        });
    } else {
        const contenedor = document.getElementById("mapa");
        contenedor.textContent = "No se pudo cargar el mapa. La lista tiene la misma información.";
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
    const pesoEstado = { operativo: 0, saturado: 1, cerrado: 2, null: 3 };
    const normalizar = (s) => (s || "").normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();

    function filtrar() {
        const q = normalizar(campos.q.value.trim());
        const recurso = campos.recurso.value;

        return centros
            .filter((c) => !q || normalizar(c.nombre + " " + (c.direccion || "")).includes(q))
            .filter((c) => !campos.zona.value || c.zonaId === campos.zona.value)
            .filter((c) => !campos.tipo.value || c.tipo === campos.tipo.value)
            .filter((c) => !recurso || (c.recursos[recurso] && c.recursos[recurso] !== "sin_stock"))
            .filter((c) => !campos.abiertos.checked || c.estado !== "cerrado")
            .sort((a, b) =>
                (recurso ? pesoNivel[a.recursos[recurso]] - pesoNivel[b.recursos[recurso]] : 0) ||
                (pesoEstado[a.estado] ?? 3) - (pesoEstado[b.estado] ?? 3) ||
                new Date(b.actualizado || 0) - new Date(a.actualizado || 0)
            );
    }

    function contarFiltrosActivos() {
        return ["q", "zona", "tipo", "recurso"].filter((k) => campos[k].value.trim()).length +
            (campos.abiertos.checked ? 1 : 0) + (campos.necesidades && campos.necesidades.checked ? 1 : 0);
    }

    function actualizarUrl() {
        const p = new URLSearchParams();
        ["q", "zona", "tipo", "recurso"].forEach((k) => { if (campos[k].value.trim()) p.set(k, campos[k].value.trim()); });
        if (campos.abiertos.checked) p.set("abiertos", "1");
        if (campos.necesidades && campos.necesidades.checked) p.set("necesidades", "1");
        const qs = p.toString();
        history.replaceState(null, "", qs ? "?" + qs : location.pathname);
    }

    const itemsPorId = new Map([...lista.querySelectorAll("li[data-centro]")].map((li) => [li.dataset.centro, li]));

    function render() {
        const encontrados = filtrar();
        const recurso = campos.recurso.value;
        const visibles = new Set(encontrados.map((c) => c.id));
        const necesidadesVisibles = necesidades.filter((n) => !campos.zona.value || n.zonaId === campos.zona.value);

        filasNecesidades.forEach((fila) => {
            fila.hidden = !!campos.zona.value && fila.dataset.zona !== campos.zona.value;
        });
        tablaNecesidades.hidden = necesidadesVisibles.length === 0;
        sinPedidosZona.hidden = necesidadesVisibles.length > 0;
        sinPedidosZona.textContent = campos.zona.value
            ? "No hay pedidos abiertos para mostrar en esta zona."
            : "No hay pedidos abiertos para mostrar.";

        // Reordena y oculta las tarjetas ya renderizadas.
        encontrados.forEach((c) => {
            const li = itemsPorId.get(c.id);
            if (li) { li.hidden = false; lista.append(li); }
        });
        itemsPorId.forEach((li, id) => { if (!visibles.has(id)) li.hidden = true; });
        sinResultados.hidden = encontrados.length > 0 || centros.length === 0;

        const sinUbicacion = encontrados.filter((c) => c.latitud === null).length;
        cantidad.textContent =
            (encontrados.length === 1 ? "1 centro encontrado" : encontrados.length + " centros encontrados") +
            (recurso ? " con " + nombreRecurso(recurso).toLowerCase() + " disponible" : "") +
            (sinUbicacion && mapa ? " (" + sinUbicacion + " sin ubicación en el mapa)" : "");

        const activos = contarFiltrosActivos();
        filtrosActivos.textContent = activos ? "(" + activos + (activos === 1 ? " activo)" : " activos)") : "";

        if (mapa) {
            puntosVisibles = [];
            marcadores.forEach((m, id) => {
                if (visibles.has(id)) { m.addTo(mapa); puntosVisibles.push(m.getLatLng()); }
                else { m.remove(); }
            });
            if (campos.necesidades) {
                capaNecesidades.clearLayers();
                circulosNecesidades.forEach(({ zonaId, circulo }) => {
                    if (!campos.zona.value || zonaId === campos.zona.value) capaNecesidades.addLayer(circulo);
                });
                if (campos.necesidades.checked) capaNecesidades.addTo(mapa); else capaNecesidades.remove();
            }
            encuadrar();
        }

        actualizarUrl();
    }

    // Si el mapa está oculto (vista Lista en celular) mide 0x0: se encuadra al mostrarlo.
    let puntosVisibles = [];
    function encuadrar() {
        if (!mapa || !mapa.getContainer().offsetWidth) return;
        const puntos = puntosVisibles.slice();
        if (campos.necesidades && campos.necesidades.checked) necesidades
            .filter((n) => !campos.zona.value || n.zonaId === campos.zona.value)
            .forEach((n) => puntos.push(L.latLng(n.latitud, n.longitud)));
        if (!puntos.length) return;
        mapa.fitBounds(L.latLngBounds(puntos).pad(0.25), { maxZoom: 15 });
    }

    // --- Vista lista / mapa (solo en pantallas chicas) ---
    const botonesVista = document.querySelectorAll(".selector-vista button");

    function cambiarVista(vista) {
        resultados.dataset.vista = vista;
        botonesVista.forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.vista === vista)));
        if (vista === "mapa" && mapa) { mapa.invalidateSize(); encuadrar(); }
    }

    botonesVista.forEach((b) => b.addEventListener("click", () => cambiarVista(b.dataset.vista)));
    if (params.get("vista") === "mapa") cambiarVista("mapa");

    // "Ver en el mapa" desde una tarjeta
    lista.addEventListener("click", (e) => {
        const boton = e.target.closest("[data-ver-mapa]");
        if (!boton || !mapa) return;
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

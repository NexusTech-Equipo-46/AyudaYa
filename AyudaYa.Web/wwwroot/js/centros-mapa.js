const centros = JSON.parse(
    document.getElementById("datos-centros").textContent
);

function normalizar(texto) {
    return (texto || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase();
}

function filtrarCentros({ texto, zona, tipo, recurso }) {
    const busqueda = normalizar(texto.trim());

    return centros.filter((centro) => {
        const coincideTexto =
            !busqueda ||
            normalizar(
                `${centro.Nombre} ${centro.Direccion || ""}`
            ).includes(busqueda);

        const coincideZona = !zona || centro.ZonaId === zona;
        const coincideTipo = !tipo || centro.TipoCodigo === tipo;

        const nivel = recurso ? centro.Recursos[recurso] : null;
        const coincideRecurso =
            !recurso || (nivel && nivel !== "sin_stock" && nivel !== "no_ofrece");

        return (
            coincideTexto &&
            coincideZona &&
            coincideTipo &&
            coincideRecurso
        );
    });
}

// Elementos de la pantalla.
const buscador = document.getElementById("buscar");
const selectorZona = document.getElementById("zona");
const selectorTipo = document.getElementById("tipo");
const selectorRecurso = document.getElementById("recurso");
const botonLimpiar = document.getElementById("limpiar-filtros");
const lista = document.getElementById("lista-centros");
const cantidad = document.getElementById("cantidad");
const sinResultados = document.getElementById("sin-resultados");

// Agrega una opción a un selector.
function agregarOpcion(selector, valor, texto) {
    const opcion = document.createElement("option");
    opcion.value = valor;
    opcion.textContent = texto;
    selector.appendChild(opcion);
}

// Completa los selectores con los datos de los centros.
function cargarOpciones() {
    const zonas = new Map();
    const tipos = new Map();
    const recursos = new Set();

    centros.forEach((centro) => {
        if (centro.ZonaId && centro.Zona) {
            zonas.set(
                centro.ZonaId,
                `${centro.Zona} - ${centro.Ciudad}`
            );
        }

        if (centro.TipoCodigo && centro.TipoNombre) {
            tipos.set(centro.TipoCodigo, centro.TipoNombre);
        }

        Object.keys(centro.Recursos || {}).forEach((recurso) => {
            recursos.add(recurso);
        });
    });

    zonas.forEach((nombre, id) => {
        agregarOpcion(selectorZona, id, nombre);
    });

    tipos.forEach((nombre, codigo) => {
        agregarOpcion(selectorTipo, codigo, nombre);
    });

    recursos.forEach((recurso) => {
        agregarOpcion(
            selectorRecurso,
            recurso,
            recurso.replaceAll("_", " ")
        );
    });
}

// Dibuja la lista según los filtros seleccionados.
function mostrarCentros() {
    const resultados = filtrarCentros({
        texto: buscador.value,
        zona: selectorZona.value,
        tipo: selectorTipo.value,
        recurso: selectorRecurso.value
    });

    lista.replaceChildren();
    cantidad.textContent = `Centros encontrados: ${resultados.length}`;
    sinResultados.hidden = resultados.length !== 0;

    resultados.forEach((centro) => {
        const elemento = document.createElement("li");
        const enlace = document.createElement("a");
        const ubicacion = document.createElement("p");

        enlace.textContent = centro.Nombre;
        enlace.href = `/Centros/Detalle/${encodeURIComponent(centro.Id)}`;

        ubicacion.textContent =
            `${centro.TipoNombre} · ${centro.Zona} · ` +
            `${centro.Direccion || "Dirección no informada"}`;

        elemento.appendChild(enlace);
        elemento.appendChild(ubicacion);
        lista.appendChild(elemento);
    });
}

// Actualiza la lista cuando cambia la búsqueda o algún filtro.
buscador.addEventListener("input", mostrarCentros);
selectorZona.addEventListener("change", mostrarCentros);
selectorTipo.addEventListener("change", mostrarCentros);
selectorRecurso.addEventListener("change", mostrarCentros);

// Limpia todos los campos.
botonLimpiar.addEventListener("click", () => {
    buscador.value = "";
    selectorZona.value = "";
    selectorTipo.value = "";
    selectorRecurso.value = "";

    mostrarCentros();
});

cargarOpciones();
const recursoInicial = new URLSearchParams(window.location.search).get("recurso");
if (recursoInicial) {
    if (!Array.from(selectorRecurso.options).some((opcion) => opcion.value === recursoInicial)) {
        agregarOpcion(selectorRecurso, recursoInicial, recursoInicial.replaceAll("_", " "));
    }
    selectorRecurso.value = recursoInicial;
}
mostrarCentros();

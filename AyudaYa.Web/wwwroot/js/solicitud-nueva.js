// Formulario "Pedir ayuda": muestra los datos de cada recurso solo cuando se
// marca, sugiere el total de personas y filtra las zonas según la ciudad.
// Sin JavaScript el formulario funciona igual (todo queda visible).
(function () {
    "use strict";

    const personas = document.getElementById("PersonasAfectadas");
    const ciudad = document.getElementById("CiudadId");
    const zona = document.getElementById("ZonaId");

    document.querySelectorAll(".recurso-opcion").forEach((bloque) => {
        const casilla = bloque.querySelector("[data-recurso-casilla]");
        const datos = bloque.querySelector("[data-recurso-datos]");
        const campoPersonas = datos.querySelector('input[type="number"]');

        function actualizar() { datos.hidden = !casilla.checked; }

        casilla.addEventListener("change", () => {
            actualizar();
            // Sugerimos el total para ahorrar tipeo.
            if (casilla.checked && !campoPersonas.value && personas && personas.value) {
                campoPersonas.value = personas.value;
            }
        });
        actualizar();
    });

    // Solo se ofrecen las zonas de la ciudad elegida.
    if (ciudad && zona) {
        const opciones = [...zona.options].filter((o) => o.value);
        function filtrar() {
            const ciudadId = ciudad.value;
            opciones.forEach((o) => { o.hidden = !!ciudadId && o.dataset.ciudad !== ciudadId; o.disabled = o.hidden; });
            if (zona.selectedOptions[0] && zona.selectedOptions[0].disabled) zona.value = "";
        }
        ciudad.addEventListener("change", filtrar);
        filtrar();
    }

    const resumen = document.getElementById("resumen-errores");
    if (resumen) {
        resumen.focus();
        resumen.addEventListener("click", (e) => {
            const enlace = e.target.closest("a[href^='#']");
            if (!enlace) return;
            const campo = document.getElementById(enlace.getAttribute("href").slice(1));
            if (campo) { e.preventDefault(); campo.focus(); }
        });
    }
})();

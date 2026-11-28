// Efectos que no se pueden hacer solo con CSS: sombra del encabezado al desplazar,
// aparición de tarjetas al llegar a ellas y conteo de las cifras de los resúmenes.
// Es un agregado: sin este archivo la página se ve y funciona igual.
(function () {
    "use strict";

    const encabezado = document.querySelector(".encabezado");
    if (encabezado) {
        const actualizar = () => encabezado.classList.toggle("desplazado", window.scrollY > 4);
        window.addEventListener("scroll", actualizar, { passive: true });
        actualizar();
    }

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    // Solo se ocultan las tarjetas que todavía están fuera de la pantalla: nada
    // de lo que ya se ve parpadea.
    if ("IntersectionObserver" in window) {
        const observador = new IntersectionObserver((entradas) => {
            entradas.forEach((entrada) => {
                if (!entrada.isIntersecting) return;
                entrada.target.classList.replace("por-revelar", "revelado");
                observador.unobserve(entrada.target);
            });
        });
        document.querySelectorAll("main .tarjeta").forEach((tarjeta) => {
            if (tarjeta.getBoundingClientRect().top <= window.innerHeight) return;
            tarjeta.classList.add("por-revelar");
            observador.observe(tarjeta);
        });
    }

    // Conteo desde cero, solo en celdas que son un número entero (es-AR: 1.234).
    document.querySelectorAll(".resumen dd").forEach((cifra) => {
        const original = cifra.textContent;
        const texto = original.trim();
        if (cifra.children.length || !/^\d{1,3}(\.\d{3})*$|^\d+$/.test(texto)) return;
        const final = Number(texto.replace(/\./g, ""));
        if (!final) return;

        const duracion = 800;
        let inicio;
        function paso(ahora) {
            inicio ??= ahora;
            const avance = Math.min(1, (ahora - inicio) / duracion);
            if (avance < 1) {
                const valor = Math.round(final * (1 - Math.pow(1 - avance, 3)));
                cifra.textContent = valor.toLocaleString("es-AR");
                requestAnimationFrame(paso);
            } else {
                cifra.textContent = original;
            }
        }
        requestAnimationFrame(paso);
    });
})();

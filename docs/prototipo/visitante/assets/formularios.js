// Validación de formularios del prototipo.
// Patrón: al enviar se muestra un resumen de errores arriba (recibe el foco y
// enlaza a cada campo) y el mensaje junto a cada campo, asociado con
// aria-describedby. Los errores se corrigen en vivo una vez mostrados.
(function () {
    "use strict";

    const A = window.AyudaYa;
    const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

    function iniciar(form, opciones) {
        const resumen = document.getElementById(opciones.resumen);
        let intentado = false;

        function errorDe(input) {
            return document.getElementById("error-" + input.id);
        }

        function limpiar(input) {
            input.removeAttribute("aria-invalid");
            errorDe(input).textContent = "";
        }

        function marcar(input, mensaje) {
            input.setAttribute("aria-invalid", "true");
            errorDe(input).textContent = mensaje;
        }

        function aplicar(errores) {
            form.querySelectorAll("input[id]").forEach((i) => { if (errorDe(i)) limpiar(i); });
            errores.forEach((e) => marcar(e.campo, e.mensaje));
        }

        function mostrarResumen(errores) {
            resumen.innerHTML =
                A.iconos.alerta +
                '<div><h2 id="titulo-resumen">' + (errores.length === 1 ? "Hay 1 dato para corregir" : "Hay " + errores.length + " datos para corregir") + "</h2>" +
                "<ul>" + errores.map((e) => '<li><a href="#' + e.campo.id + '">' + A.escapar(e.mensaje) + "</a></li>").join("") + "</ul></div>";
            resumen.hidden = false;
            resumen.focus();
        }

        resumen.addEventListener("click", (e) => {
            const enlace = e.target.closest("a[href^='#']");
            if (!enlace) return;
            e.preventDefault();
            document.getElementById(enlace.getAttribute("href").slice(1)).focus();
        });

        form.addEventListener("input", () => {
            if (!intentado) return;
            const errores = opciones.validar();
            aplicar(errores);
            if (!errores.length) resumen.hidden = true;
        });

        form.addEventListener("submit", (e) => {
            e.preventDefault();
            intentado = true;
            const errores = opciones.validar();
            aplicar(errores);
            if (errores.length) {
                mostrarResumen(errores);
            } else {
                resumen.hidden = true;
                opciones.alEnviar();
            }
        });
    }

    // Botones "Mostrar / Ocultar" contraseña.
    function iniciarMostrarContrasena() {
        document.querySelectorAll("[data-mostrar]").forEach((boton) => {
            const input = document.getElementById(boton.dataset.mostrar);
            boton.addEventListener("click", () => {
                const mostrar = input.type === "password";
                input.type = mostrar ? "text" : "password";
                boton.setAttribute("aria-pressed", String(mostrar));
                boton.firstChild.textContent = mostrar ? "Ocultar" : "Mostrar";
            });
        });
    }

    // Reemplaza el formulario por un mensaje de éxito y le pasa el foco.
    function mostrarExito(contenedor, html) {
        contenedor.innerHTML = html;
        const titulo = contenedor.querySelector("h1, h2");
        titulo.setAttribute("tabindex", "-1");
        titulo.focus();
    }

    document.addEventListener("DOMContentLoaded", iniciarMostrarContrasena);

    window.Formularios = { iniciar, mostrarExito, EMAIL };
})();

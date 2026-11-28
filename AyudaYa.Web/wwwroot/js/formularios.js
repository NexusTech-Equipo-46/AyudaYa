(function () {
    "use strict";

    const A = window.AyudaYa;
    const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

    function iniciar(form, opciones) {
        if (!form) return;
        const resumen = document.getElementById(opciones.resumen);
        if (!resumen) return;
        let intentado = false;
        let enviando = false;

        function setBotonesDeshabilitados(deshabilitar) {
            form.querySelectorAll('button[type="submit"], button:not([type]), input[type="submit"], input[type="image"]').forEach((boton) => {
                boton.disabled = deshabilitar;
            });
        }

        window.addEventListener("pageshow", (evento) => {
            if (!evento.persisted || !enviando) return;
            enviando = false;
            setBotonesDeshabilitados(false);
        });

        function errorDe(input) {
            return document.getElementById("error-" + input.id);
        }

        function limpiar(input) {
            input.removeAttribute("aria-invalid");
            const err = errorDe(input);
            if (err) err.textContent = "";
        }

        function marcar(input, mensaje) {
            input.setAttribute("aria-invalid", "true");
            const err = errorDe(input);
            if (err) err.textContent = mensaje;
        }

        function aplicar(errores) {
            form.querySelectorAll("input[id], select[id], textarea[id], fieldset[id]").forEach((i) => {
                if (errorDe(i)) limpiar(i);
            });
            errores.forEach((e) => marcar(e.campo, e.mensaje));
        }

        function mostrarResumen(errores, enfocar = true) {
            const idTitulo = opciones.tituloResumen || "titulo-resumen";
            const icono = A?.iconos?.alerta || "";
            const escapar = A?.escapar || ((t) => t);
            resumen.innerHTML =
                icono +
                '<div><h2 id="' + idTitulo + '">' + (errores.length === 1 ? "Hay 1 dato para corregir" : "Hay " + errores.length + " datos para corregir") + "</h2>" +
                "<ul>" + errores.map((e) => '<li><a href="#' + e.campo.id + '">' + escapar(e.mensaje) + "</a></li>").join("") + "</ul></div>";
            resumen.setAttribute("aria-labelledby", idTitulo);
            resumen.hidden = false;
            if (enfocar) resumen.focus();
        }

        resumen.addEventListener("click", (e) => {
            const enlace = e.target.closest("a[href^='#']");
            if (!enlace) return;
            e.preventDefault();
            document.getElementById(enlace.getAttribute("href").slice(1))?.focus();
        });

        form.addEventListener("input", () => {
            if (!intentado) return;
            const errores = opciones.validar();
            aplicar(errores);
            if (!errores.length) resumen.hidden = true;
            else if (!resumen.hidden) mostrarResumen(errores, false);
        });

        form.addEventListener("submit", (e) => {
            if (enviando) {
                e.preventDefault();
                return;
            }

            intentado = true;
            const errores = opciones.validar();
            aplicar(errores);
            if (errores.length) {
                e.preventDefault();
                mostrarResumen(errores);
            } else {
                resumen.hidden = true;
                enviando = true;
                setBotonesDeshabilitados(true);
                e.submitter?.classList.add("cargando");
                if (typeof opciones.alEnviar === "function") {
                    e.preventDefault();
                    opciones.alEnviar();
                }
            }
        });
    }

    function iniciarMostrarContrasena() {
        document.querySelectorAll("[data-mostrar]").forEach((boton) => {
            const input = document.getElementById(boton.dataset.mostrar);
            if (!input) return;
            boton.addEventListener("click", () => {
                const mostrar = input.type === "password";
                input.type = mostrar ? "text" : "password";
                boton.setAttribute("aria-pressed", String(mostrar));
                if (boton.firstChild) {
                    boton.firstChild.textContent = mostrar ? "Ocultar" : "Mostrar";
                }
            });
        });
    }

    function mostrarExito(contenedor, html) {
        if (!contenedor) return;
        contenedor.innerHTML = html;
        const titulo = contenedor.querySelector("h1, h2");
        if (titulo) {
            titulo.setAttribute("tabindex", "-1");
            titulo.focus();
        }
    }

    document.addEventListener("DOMContentLoaded", iniciarMostrarContrasena);

    window.Formularios = { iniciar, mostrarExito, EMAIL };
})();
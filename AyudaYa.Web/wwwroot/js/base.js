// Funciones comunes: clase "js" (para mostrar el botón de menú solo con JavaScript),
// menú desplegable en pantallas chicas, íconos y escape de texto.
// Se carga en el <head> sin defer para que la clase "js" esté puesta antes de pintar.
(function () {
    "use strict";

    document.documentElement.classList.add("js");

    const iconos = {
        alerta: '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M12 2.5L22.5 21h-21z" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/><path d="M12 9v5" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/><circle cx="12" cy="17.5" r="1.3" fill="currentColor"/></svg>',
        info: '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><circle cx="12" cy="12" r="10" fill="none" stroke="currentColor" stroke-width="2"/><path d="M12 11v6" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/><circle cx="12" cy="7.5" r="1.3" fill="currentColor"/></svg>'
    };

    function escapar(texto) {
        return String(texto ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
    }

    // Bloquea envíos repetidos en formularios nativos de toda la app.
    // Los formularios con validación propia gestionan su bloqueo en formularios.js.
    let formulariosEnviando = new WeakSet();
    document.addEventListener("submit", (evento) => {
        const formulario = evento.target;
        if (!(formulario instanceof HTMLFormElement) || evento.defaultPrevented || formulario.method === "dialog") return;
        if (formulariosEnviando.has(formulario)) {
            evento.preventDefault();
            return;
        }
        formulariosEnviando.add(formulario);
        evento.submitter?.classList.add("cargando");
    });
    window.addEventListener("pageshow", (evento) => {
        if (!evento.persisted) return;
        formulariosEnviando = new WeakSet();
        document.querySelectorAll(".cargando").forEach((boton) => boton.classList.remove("cargando"));
    });

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

    // Controles que reenvían su formulario al cambiar (el CSP no permite onchange en línea).
    document.addEventListener("change", (e) => {
        const control = e.target.closest("[data-autoenviar]");
        if (control && control.form) control.form.requestSubmit();
    });

    document.addEventListener("DOMContentLoaded", iniciarMenu);

    window.AyudaYa = { iconos, escapar };
})();

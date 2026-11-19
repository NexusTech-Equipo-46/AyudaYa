(function () {
    "use strict";

    // Botón "Mostrar/Ocultar" de los campos de contraseña (data-mostrar = id del campo).
    document.querySelectorAll("[data-mostrar]").forEach((boton) => {
        const campo = document.getElementById(boton.dataset.mostrar);
        if (!campo) return;
        boton.addEventListener("click", () => {
            const visible = campo.type === "password";
            campo.type = visible ? "text" : "password";
            boton.setAttribute("aria-pressed", String(visible));
            boton.firstChild.textContent = visible ? "Ocultar" : "Mostrar";
        });
    });
})();

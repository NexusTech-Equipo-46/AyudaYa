(function () {
    "use strict";

    const formulario = document.getElementById("form-login");
    const resumen = document.getElementById("resumen-errores");
    const email = document.getElementById("email");
    const contrasena = document.getElementById("contrasena");
    const mostrar = document.querySelector("[data-mostrar='contrasena']");
    let intentado = false;

    mostrar.addEventListener("click", () => {
        const visible = contrasena.type === "password";
        contrasena.type = visible ? "text" : "password";
        mostrar.setAttribute("aria-pressed", String(visible));
        mostrar.firstChild.textContent = visible ? "Ocultar" : "Mostrar";
    });
    document.getElementById("olvide").addEventListener("click", (evento) => {
        evento.preventDefault();
        document.getElementById("nota-olvide").textContent = "La recuperación de contraseña todavía no está disponible.";
    });

    function validar() {
        const errores = [];
        const valor = email.value.trim();
        if (!valor) errores.push({ campo: email, mensaje: "Escribí tu correo electrónico" });
        else if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(valor)) errores.push({ campo: email, mensaje: "Escribí un correo con el formato nombre@ejemplo.com" });
        if (!contrasena.value) errores.push({ campo: contrasena, mensaje: "Escribí tu contraseña" });
        return errores;
    }

    function actualizarCampos(errores) {
        [email, contrasena].forEach((campo) => {
            const error = errores.find((elemento) => elemento.campo === campo);
            if (error) campo.setAttribute("aria-invalid", "true");
            else campo.removeAttribute("aria-invalid");
            document.getElementById("error-" + campo.id).textContent = error ? error.mensaje : "";
        });
    }

    function mostrarResumen(errores) {
        resumen.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M12 2.5L22.5 21h-21z" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/><path d="M12 9v5" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/><circle cx="12" cy="17.5" r="1.3" fill="currentColor"/></svg><div><h2 id="titulo-resumen"></h2><ul></ul></div>';
        resumen.querySelector("h2").textContent = errores.length === 1 ? "Hay 1 dato para corregir" : `Hay ${errores.length} datos para corregir`;
        const lista = resumen.querySelector("ul");
        errores.forEach(({ campo, mensaje }) => {
            const elemento = document.createElement("li");
            const enlace = document.createElement("a");
            enlace.href = "#" + campo.id;
            enlace.textContent = mensaje;
            elemento.append(enlace);
            lista.append(elemento);
        });
        resumen.hidden = false;
        resumen.focus();
    }

    resumen.addEventListener("click", (evento) => {
        const enlace = evento.target.closest("a[href^='#']");
        if (!enlace) return;
        evento.preventDefault();
        document.getElementById(enlace.hash.slice(1))?.focus();
    });
    formulario.addEventListener("input", () => {
        if (!intentado) return;
        const errores = validar();
        actualizarCampos(errores);
        if (!errores.length) resumen.hidden = true;
    });
    formulario.addEventListener("submit", (evento) => {
        intentado = true;
        const errores = validar();
        actualizarCampos(errores);
        if (errores.length) {
            evento.preventDefault();
            mostrarResumen(errores);
        }
    });
    if (!resumen.hidden) resumen.focus();
})();

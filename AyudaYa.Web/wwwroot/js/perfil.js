(function () {
    "use strict";
    const F = window.Formularios;
    const nombre = document.getElementById("nombre");
    const dni = document.getElementById("dni");
    const datos = document.getElementById("form-datos");
    const clave = document.getElementById("form-clave");
    const actual = document.getElementById("clave-actual");
    const nueva = document.getElementById("clave-nueva");
    const confirmacion = document.getElementById("clave-confirmacion");

    F.iniciar(datos, {
        resumen: "resumen-datos",
        tituloResumen: "titulo-resumen-datos",
        validar() {
            const errores = [];
            const n = nombre.value.trim();
            if (!n) errores.push({ campo: nombre, mensaje: "Escribí tu nombre y apellido" });
            else if (n.length < 3) errores.push({ campo: nombre, mensaje: "El nombre y apellido es demasiado corto" });
            else if (n.length > 150) errores.push({ campo: nombre, mensaje: "El nombre no puede superar los 150 caracteres" });
            const d = dni.value.replace(/[.\s]/g, "");
            if (d && !/^\d{6,9}$/.test(d)) errores.push({ campo: dni, mensaje: "El DNI tiene que tener entre 6 y 9 números" });
            return errores;
        },
        alEnviar() { HTMLFormElement.prototype.submit.call(datos); }
    });

    const reglas = {
        largo: (v) => v.length >= 8 && v.length <= 100,
        mayuscula: (v) => /[A-ZÁÉÍÓÚÑ]/.test(v),
        minuscula: (v) => /[a-záéíóúñ]/.test(v),
        numero: (v) => /\d/.test(v)
    };
    function actualizarRequisitos() {
        document.querySelectorAll("#requisitos li").forEach((li) => {
            const cumple = reglas[li.dataset.regla](nueva.value);
            li.classList.toggle("cumple", cumple);
            let estado = li.querySelector(".visualmente-oculto");
            if (!estado) {
                estado = document.createElement("span");
                estado.className = "visualmente-oculto";
                li.append(estado);
            }
            estado.textContent = cumple ? " (cumplido)" : " (pendiente)";
        });
    }
    nueva.addEventListener("input", actualizarRequisitos);
    actualizarRequisitos();

    F.iniciar(clave, {
        resumen: "resumen-clave",
        tituloResumen: "titulo-resumen-clave",
        validar() {
            const errores = [];
            if (!actual.value) errores.push({ campo: actual, mensaje: "Escribí tu contraseña actual" });
            const p = nueva.value;
            if (!p) errores.push({ campo: nueva, mensaje: "Elegí una contraseña nueva" });
            else if (!Object.values(reglas).every((r) => r(p))) errores.push({ campo: nueva, mensaje: "La contraseña nueva no cumple todos los requisitos" });
            else if (p === actual.value) errores.push({ campo: nueva, mensaje: "La contraseña nueva tiene que ser distinta de la actual" });
            if (!confirmacion.value) errores.push({ campo: confirmacion, mensaje: "Repetí la contraseña nueva" });
            else if (p && confirmacion.value !== p) errores.push({ campo: confirmacion, mensaje: "Las contraseñas no coinciden" });
            return errores;
        },
        alEnviar() { HTMLFormElement.prototype.submit.call(clave); }
    });

    const resumenVisible = document.querySelector(".resumen-errores:not([hidden])");
    if (resumenVisible) resumenVisible.focus();
})();

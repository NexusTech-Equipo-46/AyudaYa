(function () {
    "use strict";
    const A = window.AyudaYa;
    const F = window.Formularios;
    const para = new URLSearchParams(location.search).get("para");
    const avisos = {
        voluntario: "Primero creá tu cuenta. Después, desde tu panel, completás “Quiero ser voluntario” con tu ciudad, zona y teléfono.",
        solicitar: "Primero creá tu cuenta. Después vas a poder cargar tu pedido de ayuda."
    };
    if (avisos[para]) {
        const aviso = document.getElementById("aviso-destino");
        aviso.innerHTML = A.iconos.info + "<p>" + avisos[para] + "</p>";
        aviso.hidden = false;
        const login = document.getElementById("enlace-login");
        const destino = new URL(login.href);
        destino.searchParams.set("volver", para === "voluntario" ? "voluntario" : "nueva-solicitud");
        login.href = destino.href;
    }

    const nombre = document.getElementById("nombre");
    const email = document.getElementById("email");
    const dni = document.getElementById("dni");
    const contrasena = document.getElementById("contrasena");
    const confirmacion = document.getElementById("confirmacion");
    const formulario = document.getElementById("form-registro");
    const resumen = document.getElementById("resumen-errores");
    const reglas = {
        largo: (v) => v.length >= 8,
        mayuscula: (v) => /[A-ZÁÉÍÓÚÑ]/.test(v),
        minuscula: (v) => /[a-záéíóúñ]/.test(v),
        numero: (v) => /\d/.test(v)
    };

    function actualizarRequisitos() {
        document.querySelectorAll("#requisitos li").forEach((li) => {
            const cumple = reglas[li.dataset.regla](contrasena.value);
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
    contrasena.addEventListener("input", actualizarRequisitos);
    actualizarRequisitos();

    F.iniciar(formulario, {
        resumen: "resumen-errores",
        validar() {
            const errores = [];
            const n = nombre.value.trim();
            if (!n) errores.push({ campo: nombre, mensaje: "Escribí tu nombre y apellido" });
            else if (n.length < 3) errores.push({ campo: nombre, mensaje: "El nombre y apellido es demasiado corto" });

            const e = email.value.trim();
            if (!e) errores.push({ campo: email, mensaje: "Escribí tu correo electrónico" });
            else if (!F.EMAIL.test(e)) errores.push({ campo: email, mensaje: "Escribí un correo con el formato nombre@ejemplo.com" });

            const d = dni.value.replace(/[.\s]/g, "");
            if (d && !/^\d{6,9}$/.test(d)) errores.push({ campo: dni, mensaje: "El DNI tiene que tener entre 6 y 9 números" });

            const p = contrasena.value;
            if (!p) errores.push({ campo: contrasena, mensaje: "Elegí una contraseña" });
            else if (!Object.values(reglas).every((r) => r(p))) errores.push({ campo: contrasena, mensaje: "La contraseña no cumple todos los requisitos" });

            if (!confirmacion.value) errores.push({ campo: confirmacion, mensaje: "Repetí la contraseña" });
            else if (p && confirmacion.value !== p) errores.push({ campo: confirmacion, mensaje: "Las contraseñas no coinciden" });
            return errores;
        },
        alEnviar() {
            // Conserva el POST y el token antifalsificación de ASP.NET.
            HTMLFormElement.prototype.submit.call(formulario);
        }
    });
    if (!resumen.hidden) resumen.focus();
})();

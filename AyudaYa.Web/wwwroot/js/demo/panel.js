(function () {
            "use strict";
            const A = window.AyudaYa;
            const AU = window.AyudaYaUsuario;
            const { usuario, solicitudes } = window.DATOS_USUARIO;

            document.getElementById("saludo").textContent = "Hola, " + usuario.nombre.split(/\s+/)[0];

            const porFecha = (a, b) => new Date(b.creado_at) - new Date(a.creado_at);
            const abiertas = solicitudes.filter(AU.esAbierta).sort(porFecha);
            const resueltas = solicitudes.filter((s) => s.estado === "resuelta");

            document.getElementById("n-abiertas").textContent = abiertas.length;
            document.getElementById("n-resueltas").textContent = resueltas.length;

            document.getElementById("lista-abiertas").innerHTML = abiertas
                .slice(0, 3)
                .map((s) => "<li>" + AU.tarjetaSolicitud(s) + "</li>")
                .join("");
            document.getElementById("vacio").hidden = abiertas.length > 0;
        })();

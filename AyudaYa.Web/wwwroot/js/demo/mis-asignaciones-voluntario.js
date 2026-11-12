(function () {
            "use strict";
            const AV = window.AyudaYaVoluntario;
            const { estado } = window.DATOS_VOLUNTARIO;

            const lista = document.getElementById("lista");
            const cantidad = document.getElementById("cantidad");
            const vacio = document.getElementById("vacio");
            const botones = document.querySelectorAll(".filtro-estado button");

            // Completadas = completadas (con o sin confirmar). Sin completar = canceladas o liberadas.
            const filtros = {
                todas: () => true,
                activas: (a) => AV.esActiva(a),
                terminadas: (a) => a.estado === "completada",
                sin_completar: (a) => a.estado === "cancelada" || a.estado === "liberada"
            };
            const sufijo = { todas: "", activas: " activas", terminadas: " completadas", sin_completar: " sin completar" };

            const inicial = new URLSearchParams(location.search).get("estado");
            let actual = filtros[inicial] ? inicial : "todas";

            // Primero lo que hay que hacer (críticas antes), después lo que espera confirmación, después lo cerrado.
            const peso = (a) => AV.esActiva(a) ? 0 : (AV.esPorConfirmar(a) ? 1 : 2);
            const critica = (a) => (AV.esActiva(a) && a.solicitud.urgencia === "critica" ? 1 : 0);
            const ordenar = (a, b) =>
                peso(a) - peso(b) ||
                critica(b) - critica(a) ||
                new Date(b.asignada_at) - new Date(a.asignada_at);

            function render() {
                const encontradas = estado.asignaciones.filter(filtros[actual]).sort(ordenar);

                botones.forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.filtro === actual)));
                lista.innerHTML = encontradas.map((a) => "<li>" + AV.tarjetaAsignacion(a) + "</li>").join("");
                cantidad.textContent = encontradas.length === 1
                    ? "1 asignación" + sufijo[actual]
                    : encontradas.length + " asignaciones" + sufijo[actual];

                vacio.hidden = encontradas.length > 0;
                if (!encontradas.length) {
                    const ninguna = !estado.asignaciones.length;
                    document.getElementById("vacio-titulo").textContent = ninguna ? "Todavía no tomaste ninguna solicitud" : "No hay asignaciones" + sufijo[actual];
                    document.getElementById("vacio-texto").textContent = ninguna
                        ? "Mirá las solicitudes disponibles en tu zona y tomá la que puedas atender."
                        : "Probá con otro filtro.";
                    document.getElementById("vacio-acciones").innerHTML = ninguna
                        ? '<a class="boton" href="' + window.AyudaYa.escapar(document.body.dataset.urlPanelVoluntario) + '#titulo-disponibles">Ver solicitudes disponibles</a>'
                        : "";
                }
            }

            botones.forEach((b) => b.addEventListener("click", () => { actual = b.dataset.filtro; render(); }));
            render();
        })();

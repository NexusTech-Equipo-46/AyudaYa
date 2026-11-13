(function () {
            "use strict";
            const AU = window.AyudaYaUsuario;
            const { solicitudes } = window.DATOS_USUARIO;

            const lista = document.getElementById("lista");
            const cantidad = document.getElementById("cantidad");
            const vacio = document.getElementById("vacio");
            const botones = document.querySelectorAll(".filtro-estado button");

            const filtros = {
                todas: () => true,
                abiertas: (s) => AU.esAbierta(s),
                cerradas: (s) => !AU.esAbierta(s)
            };
            const nombres = { todas: "", abiertas: " abiertas", cerradas: " cerradas" };

            const inicial = new URLSearchParams(location.search).get("estado");
            let actual = filtros[inicial] ? inicial : "todas";

            function render() {
                const encontradas = solicitudes
                    .filter(filtros[actual])
                    .sort((a, b) => new Date(b.creado_at) - new Date(a.creado_at));

                botones.forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.filtro === actual)));
                lista.innerHTML = encontradas.map((s) => "<li>" + AU.tarjetaSolicitud(s) + "</li>").join("");
                cantidad.textContent = encontradas.length === 1
                    ? "1 solicitud" + nombres[actual]
                    : encontradas.length + " solicitudes" + nombres[actual];

                vacio.hidden = encontradas.length > 0;
                if (!encontradas.length) {
                    const sinNinguna = !solicitudes.length;
                    document.getElementById("vacio-titulo").textContent = sinNinguna
                        ? "Todavía no pediste ayuda"
                        : "No hay solicitudes" + nombres[actual];
                    document.getElementById("vacio-texto").textContent = sinNinguna
                        ? "Cuando cargues un pedido, lo vas a ver acá con su estado."
                        : "Probá con otro filtro.";
                    document.getElementById("vacio-acciones").innerHTML = sinNinguna
                        ? '<a class="boton" href="' + window.AyudaYa.escapar(document.body.dataset.urlNuevaSolicitud) + '">Pedir ayuda</a>'
                        : "";
                }
            }

            botones.forEach((b) => b.addEventListener("click", () => { actual = b.dataset.filtro; render(); }));
            render();
        })();

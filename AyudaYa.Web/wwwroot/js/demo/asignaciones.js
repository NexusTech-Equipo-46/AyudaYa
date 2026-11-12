(function () {
            "use strict";
            const O = window.AyudaYaOrg;
            const D = window.DATOS_ORG;
            const lista = document.getElementById("lista");
            const filtros = document.querySelectorAll("[data-filtro]");

            function dibujar(filtro) {
                const orden = (a, b) => new Date(b.asignada_at) - new Date(a.asignada_at);
                const items = D.asignaciones
                    .filter((a) => filtro === "todas" || (filtro === "activas") === O.esActiva(a))
                    .sort(orden);
                lista.innerHTML = items.map((a) => "<li>" + O.tarjetaAsignacion(a) + "</li>").join("");
                document.getElementById("vacio").hidden = items.length > 0;
                document.getElementById("contador").textContent = O.plural(items.length, "asignación", "asignaciones");
            }

            filtros.forEach((b) => b.addEventListener("click", () => {
                filtros.forEach((x) => x.setAttribute("aria-pressed", String(x === b)));
                dibujar(b.dataset.filtro);
            }));

            dibujar("activas");
        })();

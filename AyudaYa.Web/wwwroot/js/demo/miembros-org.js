(function () {
            "use strict";
            const A = window.AyudaYa;
            const D = window.DATOS_ORG;
            const dialogo = document.getElementById("dialogo-baja");
            const motivo = document.getElementById("motivo");
            const avisoCaja = document.getElementById("aviso");
            let objetivo = null;

            const ESTADO_ACTIVO = '<span class="insignia ver-aprobado">' + A.iconos.check + '<span class="visualmente-oculto">Estado: </span>Activo</span>';
            const ESTADO_BAJA = '<span class="insignia asig-cancelada">' + A.iconos.cruz + '<span class="visualmente-oculto">Estado: </span>De baja</span>';
            const responsablesActivos = () => D.miembros.filter((m) => m.activo && m.rol === "responsable");

            function avisar(clase, icono, html) {
                avisoCaja.className = "alerta alerta-" + clase;
                avisoCaja.innerHTML = icono + "<p>" + html + "</p>";
            }

            function dibujar() {
                document.getElementById("filas").innerHTML = D.miembros.map((m) =>
                    '<tr' + (m.activo ? "" : ' class="fila-baja"') + '><th scope="row">' + A.escapar(m.nombre) +
                        '<span class="detalle-recurso">' + A.escapar(m.email) + "</span>" +
                        (m.motivo_baja ? '<span class="detalle-recurso">Baja: ' + A.escapar(m.motivo_baja) + "</span>" : "") + "</th>" +
                    "<td>" + (m.rol === "responsable" ? "Responsable" : "Miembro") + "</td>" +
                    "<td>" + (m.activo ? ESTADO_ACTIVO : ESTADO_BAJA) + "</td>" +
                    "<td>" + (m.activo ? '<button type="button" class="boton boton-secundario" data-baja="' + m.id + '">Dar de baja<span class="visualmente-oculto"> a ' + A.escapar(m.nombre) + "</span></button>" : "—") + "</td></tr>"
                ).join("");
            }

            document.getElementById("filas").addEventListener("click", (e) => {
                const boton = e.target.closest("[data-baja]");
                if (!boton) return;
                objetivo = D.miembros.find((m) => m.id === boton.dataset.baja);
                // Regla de la base: no se puede dejar sin responsable activo a una organización comunitaria.
                if (objetivo.rol === "responsable" && responsablesActivos().length === 1) {
                    avisar("peligro", A.iconos.alerta, "<strong>No se puede dar de baja a " + A.escapar(objetivo.nombre) + ".</strong> Es la única persona responsable y la organización es comunitaria. Primero sumen a otro responsable.");
                    avisoCaja.scrollIntoView({ block: "nearest" });
                    return;
                }
                document.getElementById("texto-baja").textContent = objetivo.nombre + " deja de actuar en nombre de la organización. No se borra: queda registrada con el motivo.";
                motivo.value = "";
                motivo.removeAttribute("aria-invalid");
                document.getElementById("error-motivo").textContent = "";
                dialogo.showModal();
                motivo.focus();
            });

            document.getElementById("cerrar-dialogo").addEventListener("click", () => dialogo.close());

            document.getElementById("form-baja").addEventListener("submit", (e) => {
                e.preventDefault();
                const texto = motivo.value.trim();
                if (!texto) {
                    motivo.setAttribute("aria-invalid", "true");
                    document.getElementById("error-motivo").textContent = "Indicá el motivo de la baja";
                    motivo.focus();
                    return;
                }
                objetivo.activo = false;
                objetivo.motivo_baja = texto;
                dialogo.close();
                dibujar();
                avisar("exito", A.iconos.ok, "<strong>Baja simulada de " + A.escapar(objetivo.nombre) + ".</strong> No se modificó ninguna cuenta real. El cambio desaparece al recargar.");
            });

            const campoEmail = document.getElementById("email");
            Formularios.iniciar(document.getElementById("form-sumar"), {
                resumen: "resumen-errores",
                validar() {
                    const v = campoEmail.value.trim();
                    if (!v) return [{ campo: campoEmail, mensaje: "Ingresá el correo electrónico" }];
                    if (!Formularios.EMAIL.test(v)) return [{ campo: campoEmail, mensaje: "El correo electrónico no parece válido" }];
                    if (D.miembros.some((m) => m.activo && m.email.toLowerCase() === v.toLowerCase())) {
                        return [{ campo: campoEmail, mensaje: "Esa persona ya es parte de la organización" }];
                    }
                    return [];
                },
                alEnviar() {
                    const v = campoEmail.value.trim();
                    D.miembros.push({ id: "m-" + Date.now(), nombre: v.split("@")[0], email: v, rol: document.getElementById("rol").value, activo: true, creado_at: new Date().toISOString(), motivo_baja: null });
                    campoEmail.value = "";
                    dibujar();
                    avisar("exito", A.iconos.ok, "<strong>Persona de ejemplo agregada.</strong> No se agregó ninguna cuenta real. El cambio desaparece al recargar.");
                }
            });

            dibujar();
        })();

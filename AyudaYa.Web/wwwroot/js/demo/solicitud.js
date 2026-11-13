(function () {
            "use strict";
            const A = window.AyudaYa;
            const AU = window.AyudaYaUsuario;
            const contenedor = document.getElementById("detalle-solicitud");
            const dialogo = document.getElementById("dialogo-cancelar");
            const motivo = document.getElementById("motivo");

            const id = contenedor.dataset.idSolicitud || new URLSearchParams(location.search).get("id") || "s-1004";
            const s = window.DATOS_USUARIO.todas.find((x) => x.id === id);

            if (!s) {
                document.title = "Solicitud no encontrada · AyudaYa";
                contenedor.innerHTML =
                    '<div class="no-encontrado"><h1>No encontramos esa solicitud</h1>' +
                    "<p>Puede que el enlace esté incompleto o que no sea una solicitud tuya.</p>" +
                    '<a class="boton" href="' + A.escapar(document.body.dataset.urlSolicitudes) + '">Ver mis solicitudes</a></div>';
                return;
            }

            document.title = "Solicitud #" + s.numero + " · AyudaYa";

            function aviso(clase, icono, html) {
                return '<div class="alerta alerta-' + clase + '">' + icono + "<p>" + html + "</p></div>";
            }

            function avisosDe(s) {
                const a = [];
                const d = AU.estados[s.estado].descripcion;
                if (s.estado === "pendiente") a.push(aviso("info", A.iconos.info, "<strong>Pendiente.</strong> " + d + " Mientras siga así podés editarla o cancelarla."));
                else if (s.estado === "asignada") a.push(aviso("info", A.iconos.info, "<strong>Asignada.</strong> " + d + " Mantené el teléfono a mano."));
                else if (s.estado === "en_atencion") a.push(aviso("info", A.iconos.info, "<strong>En atención.</strong> " + d));
                else if (s.estado === "parcial") a.push(aviso("advertencia", A.iconos.alerta, "<strong>Atención parcial.</strong> " + d));
                else if (s.estado === "resuelta") a.push(aviso("exito", A.iconos.ok, "<strong>Resuelta.</strong> " + d));
                else a.push(aviso("peligro", A.iconos.alerta, "<strong>Cancelada.</strong> Motivo: " + A.escapar(s.motivo_cancelacion)));
                if (s.urgencia === "critica" && AU.esAbierta(s)) {
                    a.push(aviso("advertencia", A.iconos.alerta, "<strong>Urgencia crítica.</strong> Si hay riesgo de vida, llamá al <a href=\"tel:911\">911</a>."));
                }
                return a.join("");
            }

            function filasRecursos(s) {
                return s.recursos.map((r) =>
                    '<tr><th scope="row">' + A.escapar(A.nombreRecurso(r.tipo_recurso)) +
                        (r.detalle ? '<span class="detalle-recurso">' + A.escapar(r.detalle) + "</span>" : "") + "</th>" +
                    "<td>" + r.personas + "</td>" +
                    "<td>" + AU.insigniaSolicitud(r.estado) + "</td>" +
                    "<td>" + AU.textoAsignado(r.asignado) + "</td></tr>"
                ).join("");
            }

            function historial(s) {
                return s.historial.slice().reverse().map((h) =>
                    "<li>" + AU.insigniaSolicitud(h.estado_nuevo) +
                    '<span class="cuando">' + A.escapar(h.actor) + " · <time datetime=\"" + h.creado_at + '" title="' + A.fechaCompleta(h.creado_at) + '">' + A.haceCuanto(h.creado_at) + "</time></span>" +
                    (h.motivo ? '<p class="motivo">' + A.escapar(h.motivo) + "</p>" : "") + "</li>"
                ).join("");
            }

            function acciones(s) {
                if (!AU.esAbierta(s)) {
                    return '<p class="texto-suave">Esta solicitud está cerrada y no se puede modificar.</p>' +
                        '<div class="acciones"><a class="boton" href="' + A.escapar(document.body.dataset.urlNuevaSolicitud) + '">Pedir ayuda de nuevo</a></div>';
                }
                return (s.estado === "pendiente"
                        ? '<p class="texto-suave">Solo podés editar la solicitud mientras está pendiente.</p>' +
                          '<div class="acciones"><button type="button" class="boton boton-secundario" id="editar">Editar solicitud</button></div>' +
                          '<p class="texto-suave" id="nota-editar" role="status" style="margin-top:.75rem"></p>'
                        : '<p class="texto-suave">Ya no se puede editar porque alguien la tomó. Si algo cambió, avisale por teléfono a quien te asiste.</p>') +
                    '<div class="acciones"><button type="button" class="boton boton-peligro" id="cancelar">Cancelar solicitud</button></div>';
            }

            function render() {
                contenedor.innerHTML =
                    '<nav class="migas" aria-label="Estás en"><ol>' +
                        '<li><a href="' + A.escapar(document.body.dataset.urlPanel) + '">Panel</a></li>' +
                        '<li><a href="' + A.escapar(document.body.dataset.urlSolicitudes) + '">Mis solicitudes</a></li>' +
                        '<li><a href="' + A.escapar(document.body.dataset.urlDetalleSolicitud) + '?id=' + encodeURIComponent(s.id) + '" aria-current="page">Solicitud #' + s.numero + "</a></li>" +
                    "</ol></nav>" +

                    '<div class="detalle-encabezado"><div>' +
                        '<h1 id="titulo-solicitud">Solicitud #' + s.numero + "</h1>" +
                        '<p class="meta">' + A.escapar(s.zona_nombre) + ", " + A.escapar(s.ciudad_nombre) + " · " + AU.plural(s.personas_afectadas, "persona afectada", "personas afectadas") + "</p>" +
                        '<p class="meta">Creada ' + A.fechaCompleta(s.creado_at) + "</p>" +
                    "</div>" +
                    '<div class="insignias">' + AU.insigniaSolicitud(s.estado) + AU.insigniaUrgencia(s.urgencia) + "</div></div>" +

                    '<div class="detalle"><div>' +
                        avisosDe(s) +
                        '<section class="tarjeta" aria-labelledby="t-recursos"><h2 id="t-recursos">Lo que pediste</h2>' +
                            '<div class="tabla-scroll" tabindex="0" role="region" aria-labelledby="t-recursos">' +
                            '<table class="tabla-recursos"><thead><tr>' +
                                '<th scope="col">Recurso</th><th scope="col">Personas</th><th scope="col">Estado</th><th scope="col">Quién atiende</th>' +
                            "</tr></thead><tbody>" + filasRecursos(s) + "</tbody></table></div></section>" +
                        (s.descripcion ? '<section class="tarjeta" aria-labelledby="t-desc"><h2 id="t-desc">Descripción</h2><p>' + A.escapar(s.descripcion) + "</p></section>" : "") +
                        '<section class="tarjeta" aria-labelledby="t-historial"><h2 id="t-historial">Historial</h2><ol class="cronologia">' + historial(s) + "</ol></section>" +
                    "</div><div>" +
                        '<section class="tarjeta" aria-labelledby="t-contacto"><h2 id="t-contacto">Tus datos de contacto</h2>' +
                            '<p class="nota-privada">' + AU.iconos.candado + "<span>Solo los ves vos, quienes te asisten y el equipo administrador.</span></p>" +
                            '<dl class="datos-contacto">' +
                                "<div><dt>Dirección</dt><dd>" + A.escapar(s.contacto.direccion) + "</dd></div>" +
                                "<div><dt>Teléfono</dt><dd>" + A.escapar(s.contacto.telefono) + "</dd></div>" +
                            "</dl></section>" +
                        '<section class="tarjeta" aria-labelledby="t-acciones"><h2 id="t-acciones">Acciones</h2>' + acciones(s) + "</section>" +
                        '<aside class="tarjeta emergencia" aria-labelledby="titulo-emergencia"><h2 id="titulo-emergencia">¿Hay riesgo de vida?</h2>' +
                            '<ul><li><a href="tel:911">911 <span>Emergencias</span></a></li><li><a href="tel:107">107 <span>SAME</span></a></li></ul></aside>' +
                    "</div></div>";

                const editar = document.getElementById("editar");
                if (editar) editar.addEventListener("click", () => {
                    document.getElementById("nota-editar").textContent =
                        "Prototipo: la edición reutiliza el formulario de Nueva solicitud con los datos cargados.";
                });
                const cancelar = document.getElementById("cancelar");
                if (cancelar) cancelar.addEventListener("click", () => {
                    motivo.value = "";
                    motivo.removeAttribute("aria-invalid");
                    document.getElementById("error-motivo").textContent = "";
                    dialogo.showModal();
                    motivo.focus();
                });
            }

            document.getElementById("cerrar-dialogo").addEventListener("click", () => dialogo.close());

            document.getElementById("form-cancelar").addEventListener("submit", (e) => {
                e.preventDefault();
                const texto = motivo.value.trim();
                if (!texto) {
                    motivo.setAttribute("aria-invalid", "true");
                    document.getElementById("error-motivo").textContent = "Contanos por qué cancelás la solicitud";
                    motivo.focus();
                    return;
                }
                // Misma regla que la base: cancelar exige motivo, quién y cuándo.
                const estadoAnterior = s.estado;
                s.estado = "cancelada";
                s.motivo_cancelacion = texto;
                s.actualizado_at = new Date().toISOString();
                s.recursos.forEach((r) => { if (r.estado !== "resuelta") r.estado = "cancelada"; });
                s.historial.push({ estado_anterior: estadoAnterior, estado_nuevo: "cancelada", actor: "Vos", motivo: texto, creado_at: s.actualizado_at });
                dialogo.close();
                render();
                const t = document.getElementById("titulo-solicitud");
                t.setAttribute("tabindex", "-1");
                t.focus();
            });

            // Al cerrar el diálogo con Esc o "No, volver", el foco vuelve al botón que lo abrió.
            dialogo.addEventListener("close", () => {
                const b = document.getElementById("cancelar");
                if (b) b.focus();
            });

            render();
        })();

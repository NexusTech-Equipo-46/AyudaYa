(function () {
            "use strict";
            const A = window.AyudaYa;
            const AV = window.AyudaYaVoluntario;
            const D = window.DATOS_VOLUNTARIO;
            const contenedor = document.getElementById("detalle-asignacion");
            const dlgCompletar = document.getElementById("dialogo-completar");
            const dlgLiberar = document.getElementById("dialogo-liberar");
            const motivo = document.getElementById("motivo");

            const id = contenedor.dataset.idAsignacion;
            const a = D.estado.asignaciones.find((x) => x.id === id);

            if (!a) {
                document.title = "Asignación no encontrada · AyudaYa";
                contenedor.innerHTML =
                    '<div class="no-encontrado"><h1>No encontramos esa asignación</h1>' +
                    "<p>Puede que el enlace esté incompleto o que no sea una asignación tuya.</p>" +
                    '<a class="boton" href="' + A.escapar(document.body.dataset.urlMisAsignaciones) + '">Ver mis asignaciones</a></div>';
                return;
            }

            document.title = AV.tituloAsignacion(a) + " · AyudaYa";

            // Mensaje de lo último que hizo (se muestra y recibe el foco después de renderizar).
            let mensaje = null;
            let abridor = null;

            const aviso = (clase, icono, html) => '<div class="alerta alerta-' + clase + '">' + icono + "<p>" + html + "</p></div>";

            function avisosDe() {
                const quien = A.escapar(a.solicitante_nombre);
                const out = [];
                switch (AV.estadoVisible(a)) {
                    case "asignada":
                        out.push(aviso("info", A.iconos.info, "<strong>Tomaste esta asignación.</strong> Comunicate con " + quien + " y, cuando empieces, tocá “Iniciar atención”."));
                        break;
                    case "en_atencion":
                        out.push(aviso("info", A.iconos.info, "<strong>Estás atendiendo esta asignación.</strong> Cuando termines, marcala como completada."));
                        break;
                    case "completada":
                        out.push(aviso("advertencia", A.iconos.alerta, "<strong>Falta la confirmación.</strong> Esperamos que " + quien + " confirme que recibió la ayuda. Mientras tanto seguís viendo sus datos de contacto."));
                        break;
                    case "confirmada":
                        out.push(aviso("exito", A.iconos.ok, "<strong>" + quien + " confirmó la ayuda.</strong> ¡Gracias por tu trabajo!"));
                        break;
                    case "cancelada":
                        out.push(aviso("peligro", A.iconos.alerta, "<strong>La solicitud se canceló.</strong> " + quien + " la canceló: no hace falta que hagas nada más. Motivo: " + A.escapar(a.motivo_cancelacion)));
                        break;
                    default:
                        out.push(aviso("info", A.iconos.info, "<strong>Liberaste esta asignación.</strong> La solicitud volvió a estar disponible para otras personas."));
                }
                if (a.solicitud.urgencia === "critica" && AV.esActiva(a)) {
                    out.push(aviso("advertencia", A.iconos.alerta, "<strong>Urgencia crítica.</strong> Si hay riesgo de vida, llamá al <a href=\"tel:911\">911</a> antes que nada."));
                }
                return out.join("");
            }

            function contacto() {
                if (!AV.veContacto(a)) {
                    return '<div class="contacto-oculto"><p>' + AV.iconos.candado.replace("<svg", '<svg width="16" height="16"') +
                        " Ya no tenés acceso a la dirección ni al teléfono de " + A.escapar(AV.nombreCorto(a.solicitante_nombre)) +
                        ". Se muestran solo mientras la asignación está activa o sin confirmar.</p></div>";
                }
                const tel = a.contacto.telefono.replace(/\D/g, "");
                const mapa = "https://www.google.com/maps/search/?api=1&query=" + encodeURIComponent(a.contacto.direccion);
                return '<p class="nota-privada">' + AV.iconos.candado +
                        "<span>Son datos privados: usalos solo para esta asistencia. Dejás de verlos cuando termina.</span></p>" +
                    '<dl class="datos-contacto">' +
                        "<div><dt>Nombre</dt><dd>" + A.escapar(a.solicitante_nombre) + "</dd></div>" +
                        "<div><dt>Dirección</dt><dd>" + A.escapar(a.contacto.direccion) + "</dd></div>" +
                        '<div><dt>Teléfono</dt><dd><a href="tel:' + tel + '">' + A.escapar(a.contacto.telefono) + "</a></dd></div>" +
                    "</dl>" +
                    '<div class="acciones"><a class="boton boton-secundario" href="' + mapa + '" target="_blank" rel="noopener">Cómo llegar<span class="visualmente-oculto"> (se abre en otra pestaña)</span></a></div>';
            }

            function acciones() {
                const e = AV.estadoVisible(a);
                const liberar = '<button type="button" class="boton boton-secundario" id="liberar">Liberar asignación</button>';
                if (e === "asignada") {
                    return '<p class="texto-suave">Cuando estés en camino o ya hayas hablado con la persona, empezá la atención.</p>' +
                        '<div class="acciones"><button type="button" class="boton" id="iniciar">Iniciar atención</button>' + liberar + "</div>";
                }
                if (e === "en_atencion") {
                    return '<p class="texto-suave">Cuando termines de asistir, marcala como completada.</p>' +
                        '<div class="acciones"><button type="button" class="boton" id="completar">Marcar como completada</button>' + liberar + "</div>";
                }
                if (e === "completada") return '<p class="texto-suave">Ya no hay nada para hacer: esperamos la confirmación de ' + A.escapar(AV.nombreCorto(a.solicitante_nombre)) + ".</p>";
                return '<p class="texto-suave">Esta asignación está cerrada y no se puede modificar.</p>' +
                    '<div class="acciones"><a class="boton" href="' + A.escapar(document.body.dataset.urlPanelVoluntario) + '#titulo-disponibles">Tomar otra solicitud</a></div>';
            }

            function filaRecurso() {
                const r = a.recurso;
                return '<tr><th scope="row">' + A.escapar(A.nombreRecurso(r.tipo_recurso)) +
                    (r.detalle ? '<span class="detalle-recurso">' + A.escapar(r.detalle) + "</span>" : "") + "</th>" +
                    "<td>" + r.personas + "</td><td>" + AV.insigniaAsignacion(a) + "</td></tr>";
            }

            function historial() {
                return AV.historialDe(a).map((h) =>
                    "<li>" + AV.insigniaEvento(h.estado) +
                    '<span class="cuando">' + A.escapar(h.actor) + ' · <time datetime="' + h.cuando + '" title="' + A.fechaCompleta(h.cuando) + '">' + A.haceCuanto(h.cuando) + "</time></span>" +
                    (h.motivo ? '<p class="motivo">' + A.escapar(h.motivo) + "</p>" : "") + "</li>"
                ).join("");
            }

            function render() {
                const s = a.solicitud;
                contenedor.innerHTML =
                    '<nav class="migas" aria-label="Estás en"><ol>' +
                        '<li><a href="' + A.escapar(document.body.dataset.urlPanelVoluntario) + '">Panel</a></li>' +
                        '<li><a href="' + A.escapar(document.body.dataset.urlMisAsignaciones) + '">Mis asignaciones</a></li>' +
                        '<li><a href="' + A.escapar(document.body.dataset.urlAsignacionVoluntario) + '?id=' + encodeURIComponent(a.id) + '" aria-current="page">' + A.escapar(AV.tituloAsignacion(a)) + "</a></li>" +
                    "</ol></nav>" +

                    '<div class="detalle-encabezado"><div>' +
                        '<h1 id="titulo-asignacion" tabindex="-1">' + A.escapar(AV.tituloAsignacion(a)) + "</h1>" +
                        '<p class="meta">Solicitud #' + s.numero + " · " + A.escapar(s.zona_nombre) + ", " + A.escapar(s.ciudad_nombre) + "</p>" +
                        '<p class="meta">Tomada ' + A.fechaCompleta(a.asignada_at) + "</p>" +
                    "</div>" +
                    '<div class="insignias">' + AV.insigniaAsignacion(a) + AV.insigniaUrgencia(s.urgencia) + "</div></div>" +

                    '<div class="detalle"><div>' +
                        '<div class="alerta alerta-exito" id="mensaje-accion" role="status" tabindex="-1" hidden></div>' +
                        avisosDe() +
                        '<section class="tarjeta" aria-labelledby="t-tarea"><h2 id="t-tarea">Qué hay que llevar o hacer</h2>' +
                            '<div class="tabla-scroll" tabindex="0" role="region" aria-labelledby="t-tarea">' +
                            '<table class="tabla-recursos"><thead><tr><th scope="col">Recurso</th><th scope="col">Personas</th><th scope="col">Estado</th></tr></thead>' +
                            "<tbody>" + filaRecurso() + "</tbody></table></div>" +
                            '<p class="texto-suave" style="margin:1rem 0 0">La solicitud completa es para ' + AV.plural(s.personas_afectadas, "persona", "personas") + ".</p>" +
                        "</section>" +
                        (s.descripcion ? '<section class="tarjeta" aria-labelledby="t-desc"><h2 id="t-desc">Lo que contó quien pidió ayuda</h2><p>' + A.escapar(s.descripcion) + "</p></section>" : "") +
                        '<section class="tarjeta" aria-labelledby="t-historial"><h2 id="t-historial">Historial</h2><ol class="cronologia">' + historial() + "</ol></section>" +
                    "</div><div>" +
                        '<section class="tarjeta" aria-labelledby="t-contacto"><h2 id="t-contacto">Contacto de quien pidió ayuda</h2>' + contacto() + "</section>" +
                        '<section class="tarjeta" aria-labelledby="t-acciones"><h2 id="t-acciones">Acciones</h2><p class="texto-suave">Demostración: los cambios y avisos son ficticios y no afectan solicitudes reales.</p><div class="acciones-asignacion">' + acciones() + "</div></section>" +
                        '<aside class="tarjeta emergencia" aria-labelledby="titulo-emergencia"><h2 id="titulo-emergencia">¿Hay riesgo de vida?</h2>' +
                            '<ul><li><a href="tel:911">911 <span>Emergencias</span></a></li><li><a href="tel:107">107 <span>SAME</span></a></li></ul></aside>' +
                    "</div></div>";

                const iniciar = document.getElementById("iniciar");
                if (iniciar) iniciar.addEventListener("click", () => {
                    a.estado = "en_atencion";
                    a.iniciada_at = new Date().toISOString();
                    D.guardar();
                    mensaje = "Atención iniciada en la demostración. No se modificó ninguna asignación real.";
                    render();
                });

                const completar = document.getElementById("completar");
                if (completar) completar.addEventListener("click", () => {
                    abridor = completar;
                    document.getElementById("nombre-completar").textContent = a.solicitante_nombre;
                    dlgCompletar.showModal();
                });

                const liberar = document.getElementById("liberar");
                if (liberar) liberar.addEventListener("click", () => {
                    abridor = liberar;
                    motivo.value = "";
                    motivo.removeAttribute("aria-invalid");
                    document.getElementById("error-motivo").textContent = "";
                    dlgLiberar.showModal();
                    motivo.focus();
                });

                if (mensaje) {
                    const caja = document.getElementById("mensaje-accion");
                    caja.innerHTML = A.iconos.ok + "<p>" + A.escapar(mensaje) + "</p>";
                    caja.hidden = false;
                    caja.focus();
                    mensaje = null;
                }
            }

            // ---------- Diálogos ----------
            document.querySelectorAll("[data-cerrar]").forEach((b) => b.addEventListener("click", () => b.closest("dialog").close()));

            document.getElementById("form-completar").addEventListener("submit", (e) => {
                e.preventDefault();
                a.estado = "completada";
                a.completada_at = new Date().toISOString();
                D.guardar();
                mensaje = "Asignación marcada como completada en la demostración. No se enviaron avisos ni se modificaron datos reales.";
                abridor = null;
                dlgCompletar.close();
                render();
            });

            document.getElementById("form-liberar").addEventListener("submit", (e) => {
                e.preventDefault();
                const texto = motivo.value.trim();
                if (!texto) {
                    // Igual que la base: liberar exige motivo.
                    motivo.setAttribute("aria-invalid", "true");
                    document.getElementById("error-motivo").textContent = "Contanos por qué liberás la asignación";
                    motivo.focus();
                    return;
                }
                a.estado = "liberada";
                a.liberada_at = new Date().toISOString();
                a.motivo_liberacion = texto;
                D.guardar();
                mensaje = "Asignación liberada en la demostración. No se modificó ninguna solicitud real.";
                abridor = null;
                dlgLiberar.close();
                render();
            });

            // Si se cierra con Esc o “No, volver”, el foco vuelve al botón que lo abrió.
            [dlgCompletar, dlgLiberar].forEach((d) => d.addEventListener("close", () => {
                if (abridor && document.body.contains(abridor)) abridor.focus();
            }));

            render();
        })();

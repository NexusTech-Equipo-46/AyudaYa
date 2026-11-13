(function () {
            "use strict";
            const A = window.AyudaYa;
            const AV = window.AyudaYaVoluntario;
            const D = window.DATOS_VOLUNTARIO;
            const { voluntario, estado, disponibles } = D;

            const $ = (id) => document.getElementById(id);
            const ampliar = $("ampliar");
            const botonPausa = $("boton-pausa");

            $("saludo").textContent = "Hola, " + voluntario.nombre.split(/\s+/)[0];
            $("subtitulo").textContent = "Voluntario/a en " + voluntario.zona_nombre + ", " + voluntario.ciudad_nombre;
            $("texto-ampliar").textContent = "Ver también otras zonas de " + voluntario.ciudad_nombre;
            $("d-ciudad").textContent = voluntario.ciudad_nombre;
            $("d-zona").textContent = voluntario.zona_nombre;
            $("d-telefono").textContent = voluntario.telefono_contacto;

            const porUrgenciaYFecha = (a, b) =>
                (b.solicitud.urgencia === "critica") - (a.solicitud.urgencia === "critica") ||
                new Date(b.asignada_at) - new Date(a.asignada_at);

            // ---------- Disponibles ----------
            const tomado = (s, r) => estado.tomados.includes(s.id + "|" + r.tipo_recurso);

            function visibles() {
                return disponibles
                    .filter((s) => ampliar.checked || s.zona_id === voluntario.zona_id)
                    .filter((s) => s.recursos.some((r) => r.estado === "pendiente" && !tomado(s, r)))
                    .sort((a, b) => (b.urgencia === "critica") - (a.urgencia === "critica") || new Date(b.creado_at) - new Date(a.creado_at));
            }

            function tarjetaDisponible(s) {
                const filas = s.recursos.map((r) => {
                    const nombre = A.escapar(A.nombreRecurso(r.tipo_recurso));
                    const detalle = (r.detalle ? " · " + A.escapar(r.detalle) : "");
                    const que = '<span class="que"><strong>' + nombre + "</strong><span>" + AV.plural(r.personas, "persona", "personas") + detalle + "</span></span>";
                    if (r.estado === "pendiente" && !tomado(s, r)) {
                        return "<li>" + que +
                            '<button type="button" class="boton boton-secundario" data-tomar="' + s.id + "|" + r.tipo_recurso + '"' + (estado.activo ? "" : " disabled") + ">Tomar" +
                            '<span class="visualmente-oculto"> ' + nombre.toLowerCase() + " de la solicitud " + s.numero + "</span></button></li>";
                    }
                    return "<li>" + que + '<span class="insignia estado-recurso-tomado">' + AV.iconos.asignada + (tomado(s, r) ? "Lo tomaste vos" : "Ya lo atiende alguien") + "</span></li>";
                }).join("");

                return '<article class="tarjeta disponible" aria-labelledby="titulo-' + s.id + '">' +
                    '<div class="cabeza"><h3 id="titulo-' + s.id + '">Solicitud #' + s.numero + "</h3>" +
                        '<div class="insignias">' + AV.insigniaUrgencia(s.urgencia) + "</div></div>" +
                    '<p class="meta">' + A.escapar(s.zona_nombre) + " · " + AV.plural(s.personas_afectadas, "persona afectada", "personas afectadas") +
                        " · creada " + A.haceCuanto(s.creado_at) + "</p>" +
                    (s.descripcion ? '<p class="descripcion">' + A.escapar(s.descripcion) + "</p>" : "") +
                    '<ul class="recursos-tomar" aria-label="Recursos que necesita">' + filas + "</ul>" +
                "</article>";
            }

            // ---------- Render ----------
            function render() {
                const mias = estado.asignaciones;
                const activas = mias.filter(AV.esActiva).sort(porUrgenciaYFecha);

                $("n-activas").textContent = activas.length;
                $("n-confirmar").textContent = mias.filter(AV.esPorConfirmar).length;
                $("n-terminadas").textContent = mias.filter((a) => AV.estadoVisible(a) === "confirmada").length;

                $("lista-activas").innerHTML = activas.slice(0, 3).map((a) => "<li>" + AV.tarjetaAsignacion(a) + "</li>").join("");
                $("vacio-activas").hidden = activas.length > 0;

                // Pausa
                $("aviso-pausa").hidden = estado.activo;
                if (!estado.activo) {
                    $("aviso-pausa").innerHTML = A.iconos.alerta +
                        "<p><strong>Estás en pausa.</strong> No podés tomar solicitudes nuevas. Tus asignaciones activas siguen vigentes.</p>";
                }
                botonPausa.textContent = estado.activo ? "Pausar mi voluntariado" : "Volver a estar disponible";
                botonPausa.setAttribute("aria-pressed", String(!estado.activo));
                $("d-estado").textContent = estado.activo ? "Disponible" : "En pausa";

                // Disponibles
                const lista = visibles();
                $("lista-disponibles").innerHTML = lista.map((s) => "<li>" + tarjetaDisponible(s) + "</li>").join("");
                $("vacio-disponibles").hidden = lista.length > 0;
                $("texto-disponibles").textContent = ampliar.checked
                    ? "Pedidos pendientes en toda la ciudad. Los críticos van primero."
                    : "Pedidos pendientes en " + voluntario.zona_nombre + ". Los críticos van primero.";
            }

            // ---------- Eventos ----------
            botonPausa.addEventListener("click", () => {
                estado.activo = !estado.activo;
                D.guardar();
                render();
            });

            ampliar.addEventListener("change", render);

            $("lista-disponibles").addEventListener("click", (e) => {
                const b = e.target.closest("[data-tomar]");
                if (!b || b.disabled) return;
                const [idSolicitud, codigo] = b.dataset.tomar.split("|");
                const s = disponibles.find((x) => x.id === idSolicitud);
                const r = s.recursos.find((x) => x.tipo_recurso === codigo);

                // En la app real esto crea la fila en "asignacion" y el servidor devuelve el contacto.
                const nueva = {
                    id: "a-" + (2100 + estado.tomados.length), estado: "asignada", asignada_at: new Date().toISOString(),
                    iniciada_at: null, completada_at: null, confirmada_at: null,
                    cancelada_at: null, cancelada_por_nombre: null, motivo_cancelacion: null,
                    liberada_at: null, motivo_liberacion: null,
                    solicitud: { id: s.id, numero: s.numero, urgencia: s.urgencia, zona_id: s.zona_id, zona_nombre: s.zona_nombre,
                        ciudad_nombre: s.ciudad_nombre, personas_afectadas: s.personas_afectadas, descripcion: s.descripcion, creado_at: s.creado_at },
                    solicitante_nombre: s._privado.solicitante_nombre,
                    recurso: { tipo_recurso: r.tipo_recurso, personas: r.personas, detalle: r.detalle },
                    contacto: { direccion: s._privado.direccion, telefono: s._privado.telefono }
                };
                estado.asignaciones.push(nueva);
                estado.tomados.push(idSolicitud + "|" + codigo);
                D.guardar();
                render();

                const aviso = $("aviso-tomada");
                aviso.innerHTML = A.iconos.ok + "<p><strong>Asignación de ejemplo: " + A.escapar(AV.tituloAsignacion(nueva).toLowerCase()) + "</strong> de la solicitud #" + s.numero + ". No se tomó ningún pedido real. " +
                    '<a href="' + A.escapar(document.body.dataset.urlAsignacionVoluntario) + '?id=' + encodeURIComponent(nueva.id) + '">Ver los datos de contacto y empezar</a>.</p>';
                aviso.hidden = false;
                aviso.focus();
            });

            render();
        })();

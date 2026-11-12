(function () {
            "use strict";
            const A = window.AyudaYa;
            const F = window.Formularios;
            const { ciudad, zonas, tiposRecurso } = window.DATOS;

            const zona = document.getElementById("zona");
            const personas = document.getElementById("personas");
            const direccion = document.getElementById("direccion");
            const telefono = document.getElementById("telefono");
            const grupo = document.getElementById("recursos-grupo");
            const lista = document.getElementById("recursos-lista");

            document.getElementById("nombre-ciudad").textContent = ciudad.nombre;
            zonas.forEach((z) => {
                const o = document.createElement("option");
                o.value = z.id;
                o.textContent = z.nombre;
                zona.append(o);
            });

            // Un bloque por tipo_recurso: casilla + personas + detalle (solicitud_recurso).
            lista.innerHTML = tiposRecurso.map((t) =>
                '<div class="recurso-opcion">' +
                    '<label class="check"><input type="checkbox" id="r-' + t.codigo + '" name="recurso" value="' + t.codigo + '" aria-controls="datos-' + t.codigo + '"> ' + A.escapar(t.nombre) + "</label>" +
                    '<div class="recurso-datos" id="datos-' + t.codigo + '" hidden>' +
                        '<div class="campo"><label for="p-' + t.codigo + '">Personas que lo necesitan</label>' +
                            '<input type="number" id="p-' + t.codigo + '" min="1" step="1" inputmode="numeric" aria-describedby="error-p-' + t.codigo + '">' +
                            '<p class="error-campo" id="error-p-' + t.codigo + '"></p></div>' +
                        '<div class="campo"><label for="d-' + t.codigo + '">Detalle <span class="opcional">(opcional)</span></label>' +
                            '<input type="text" id="d-' + t.codigo + '" maxlength="200">' +
                            '<p class="error-campo" id="error-d-' + t.codigo + '"></p></div>' +
                    "</div>" +
                "</div>"
            ).join("");

            const casillas = [...lista.querySelectorAll('input[type="checkbox"]')];

            lista.addEventListener("change", (e) => {
                const c = e.target.closest('input[type="checkbox"]');
                if (!c) return;
                const datos = document.getElementById("datos-" + c.value);
                datos.hidden = !c.checked;
                const p = document.getElementById("p-" + c.value);
                // Sugerimos el total de personas afectadas para ahorrar tipeo.
                if (c.checked && !p.value && personas.value) {
                    p.value = personas.value;
                    p.dispatchEvent(new Event("input", { bubbles: true }));
                }
            });

            F.iniciar(document.getElementById("form-solicitud"), {
                resumen: "resumen-errores",
                validar() {
                    const errores = [];
                    const entero = (v) => /^\d+$/.test(String(v).trim()) ? Number(v) : NaN;

                    if (!zona.value) errores.push({ campo: zona, mensaje: "Elegí la zona donde estás" });

                    const total = entero(personas.value);
                    if (!personas.value.trim()) errores.push({ campo: personas, mensaje: "Indicá cuántas personas necesitan ayuda" });
                    else if (!(total > 0)) errores.push({ campo: personas, mensaje: "Escribí un número entero mayor a cero" });

                    const marcadas = casillas.filter((c) => c.checked);
                    if (!marcadas.length) errores.push({ campo: grupo, mensaje: "Elegí al menos un recurso que necesitás" });

                    marcadas.forEach((c) => {
                        const p = document.getElementById("p-" + c.value);
                        const nombre = A.nombreRecurso(c.value).toLowerCase();
                        const n = entero(p.value);
                        if (!p.value.trim() || !(n > 0)) {
                            errores.push({ campo: p, mensaje: "Indicá cuántas personas necesitan " + nombre });
                        } else if (total > 0 && n > total) {
                            errores.push({ campo: p, mensaje: "Las personas que necesitan " + nombre + " no pueden ser más que las afectadas (" + total + ")" });
                        }
                    });

                    if (!direccion.value.trim()) errores.push({ campo: direccion, mensaje: "Escribí tu dirección para que puedan encontrarte" });

                    const digitos = telefono.value.replace(/\D/g, "");
                    if (!telefono.value.trim()) errores.push({ campo: telefono, mensaje: "Escribí un teléfono de contacto" });
                    else if (digitos.length < 8 || digitos.length > 13) errores.push({ campo: telefono, mensaje: "Escribí un teléfono válido, con característica" });

                    return errores;
                },
                alEnviar() {
                    // Solo se conserva un resumen local para mostrar la demostración.
                    try {
                        sessionStorage.setItem("ayudaya-ultima-solicitud", JSON.stringify({
                            urgencia: document.querySelector('input[name="urgencia"]:checked').value,
                            zona: zona.options[zona.selectedIndex].text,
                            personas: Number(personas.value),
                            recursos: casillas.filter((c) => c.checked).map((c) => ({
                                tipo_recurso: c.value,
                                personas: Number(document.getElementById("p-" + c.value).value)
                            }))
                        }));
                    } catch (e) { /* Sin almacenamiento se muestra el ejemplo del prototipo. */ }
                    location.href = document.body.dataset.urlPendiente;
                }
            });
        })();

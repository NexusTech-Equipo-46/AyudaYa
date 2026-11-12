(function () {
            "use strict";
            const A = window.AyudaYa;
            const F = window.Formularios;
            const { ciudad, zonas } = window.DATOS;

            const selCiudad = document.getElementById("ciudad");
            const selZona = document.getElementById("zona");
            const telefono = document.getElementById("telefono");
            const acepto = document.getElementById("acepto");

            // Hoy hay una sola ciudad cargada; el select ya queda listo para varias.
            const o = document.createElement("option");
            o.value = ciudad.id;
            o.textContent = ciudad.nombre + ", " + ciudad.provincia;
            selCiudad.append(o);
            selCiudad.value = ciudad.id;

            zonas.forEach((z) => {
                const op = document.createElement("option");
                op.value = z.id;
                op.textContent = z.nombre;
                selZona.append(op);
            });

            F.iniciar(document.getElementById("form-voluntario"), {
                resumen: "resumen-errores",
                validar() {
                    const errores = [];
                    if (!selCiudad.value) errores.push({ campo: selCiudad, mensaje: "Elegí tu ciudad" });

                    const digitos = telefono.value.replace(/\D/g, "");
                    if (!telefono.value.trim()) errores.push({ campo: telefono, mensaje: "Escribí un teléfono de contacto" });
                    else if (digitos.length < 8 || digitos.length > 13) errores.push({ campo: telefono, mensaje: "Escribí un teléfono válido, con característica" });

                    if (!acepto.checked) errores.push({ campo: acepto, mensaje: "Tenés que aceptar para poder sumarte" });
                    return errores;
                },
                alEnviar() {
                    const zonaTexto = selZona.value ? selZona.options[selZona.selectedIndex].text : "toda la ciudad";
                    F.mostrarExito(document.getElementById("contenedor-voluntariado"),
                        '<div class="alerta alerta-exito">' + A.iconos.ok +
                        "<div><h1 style=\"font-size:1.75rem\">Demostración completada</h1>" +
                        "<p>Ejemplo de voluntariado para <strong>" + A.escapar(zonaTexto) + "</strong> en " + A.escapar(ciudad.nombre) + ".</p>" +
                        "<p><strong>Esto es una demostración: no se registró una inscripción real.</strong></p></div></div>" +
                        '<div class="acciones"><a class="boton" href="' + A.escapar(document.body.dataset.urlVolver) + '">' + A.escapar(document.body.dataset.textoVolver) + '</a>' +
                        '<a class="boton boton-secundario" href="' + A.escapar(document.body.dataset.urlCentros) + '">Ver centros de ayuda</a></div>');
                }
            });
            document.getElementById("enviar-voluntario").type = "submit";
        })();

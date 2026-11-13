(function () {
            "use strict";
            const A = window.AyudaYa;
            const O = window.AyudaYaOrg;
            const D = window.DATOS_ORG;
            const dialogo = document.getElementById("dialogo-centro");
            const form = document.getElementById("form-centro");
            const campo = (id) => document.getElementById(id);

            function tarjeta(c) {
                const tipo = window.DATOS.tiposCentro.find((t) => t.codigo === c.tipo_centro_codigo).nombre;
                const motivo = c.motivo_estado
                    ? '<div class="alerta alerta-peligro">' + A.iconos.alerta + "<p><strong>Motivo:</strong> " + A.escapar(c.motivo_estado) + "</p></div>" : "";
                const pendiente = c.estado_verificacion === "pendiente"
                    ? '<div class="alerta alerta-info">' + A.iconos.info + (c.demo_nuevo
                        ? "<p>Ejemplo pendiente de aprobación. No se envió a ningún administrador.</p></div>"
                        : "<p>Un administrador lo está revisando. Todavía no aparece en el mapa.</p></div>") : "";
                const aprobado = c.estado_verificacion === "aprobado";
                return '<li><article class="tarjeta centro-org" aria-labelledby="titulo-' + c.id + '">' +
                    '<h3 id="titulo-' + c.id + '">' + A.escapar(c.nombre) + "</h3>" +
                    '<p class="meta">' + A.escapar(tipo) + " · " + A.escapar(c.zona_nombre) + "</p>" +
                    '<p class="meta">' + A.escapar(c.direccion) + "</p>" +
                    '<p class="meta"><strong>Horario:</strong> ' + A.escapar(O.textoHorario(c.horarios)) + "</p>" +
                    '<div class="insignias">' + O.insigniaVerificacion(c.estado_verificacion) + (aprobado ? A.insigniaEstado(c.estado_operativo) : "") + "</div>" +
                    motivo + pendiente +
                    '<div class="acciones">' + (c.demo_nuevo
                        ? '<button type="button" class="boton boton-secundario" disabled title="El centro de ejemplo solo existe en esta pantalla">Recursos<span class="visualmente-oculto"> de ' + A.escapar(c.nombre) + '</span></button>'
                        : '<a class="boton boton-secundario" href="' + A.escapar(document.body.dataset.urlRecursosOrg) + '?id=' + encodeURIComponent(c.id) + '">Recursos<span class="visualmente-oculto"> de ' + A.escapar(c.nombre) + '</span></a>') +
                    (aprobado ? '<a class="boton boton-secundario" href="' + A.escapar(document.body.dataset.urlCentroEjemplo) + '?id=' + encodeURIComponent(c.id) + '">Ver como visitante<span class="visualmente-oculto">: ' + A.escapar(c.nombre) + "</span></a>" : "") +
                    "</div></article></li>";
            }

            function dibujar() {
                campo("lista-centros").innerHTML = D.centros.map(tarjeta).join("");
            }

            campo("tipo").innerHTML = '<option value="">Elegí un tipo</option>' +
                window.DATOS.tiposCentro.map((t) => '<option value="' + t.codigo + '">' + t.nombre + "</option>").join("");
            campo("zona").innerHTML = '<option value="">Elegí una zona</option>' +
                window.DATOS.zonas.map((z) => '<option value="' + z.id + '">' + z.nombre + "</option>").join("");

            const nombresDias = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];
            document.querySelector(".dias-semana").innerHTML = nombresDias.map((n, i) =>
                '<label class="opcion"><input type="checkbox" id="dia-' + (i + 1) + '" name="dia" value="' + (i + 1) + '"' + (i === 0 ? ' aria-describedby="error-dia-1"' : "") + "> " + n + "</label>").join("");

            const diasMarcados = () => Array.from(document.querySelectorAll("input[name=dia]:checked")).map((i) => Number(i.value));

            // Con 24 horas se deshabilitan días y horas (todo_el_dia no admite abre ni cierra).
            campo("todo-el-dia").addEventListener("change", (e) => {
                campo("horario-turno").hidden = e.target.checked;
            });

            // Reglas de la base: nombre único dentro de la organización (sin distinguir mayúsculas).
            function validar() {
                const errores = [];
                const nombre = campo("nombre").value.trim();
                if (!nombre) errores.push({ campo: campo("nombre"), mensaje: "Ingresá el nombre del centro" });
                else if (D.centros.some((c) => c.nombre.toLowerCase() === nombre.toLowerCase())) {
                    errores.push({ campo: campo("nombre"), mensaje: "Ya tienen un centro con ese nombre" });
                }
                if (!campo("tipo").value) errores.push({ campo: campo("tipo"), mensaje: "Elegí el tipo de centro" });
                if (!campo("zona").value) errores.push({ campo: campo("zona"), mensaje: "Elegí la zona" });
                if (!campo("direccion").value.trim()) errores.push({ campo: campo("direccion"), mensaje: "Ingresá la dirección" });
                if (!campo("telefono").value.trim()) errores.push({ campo: campo("telefono"), mensaje: "Ingresá un teléfono de contacto" });
                // centro_horario: abre < cierra, y un turno necesita al menos un día.
                if (!campo("todo-el-dia").checked) {
                    const dias = diasMarcados();
                    const abre = campo("abre").value, cierra = campo("cierra").value;
                    if (dias.length || abre || cierra) {
                        if (!dias.length) errores.push({ campo: campo("dia-1"), mensaje: "Marcá los días de atención" });
                        if (!abre) errores.push({ campo: campo("abre"), mensaje: "Ingresá la hora de apertura" });
                        if (!cierra) errores.push({ campo: campo("cierra"), mensaje: "Ingresá la hora de cierre" });
                        if (abre && cierra && abre >= cierra) errores.push({ campo: campo("cierra"), mensaje: "La hora de cierre tiene que ser posterior a la de apertura" });
                    }
                }
                return errores;
            }

            function horariosDelForm() {
                if (campo("todo-el-dia").checked) {
                    return [1, 2, 3, 4, 5, 6, 7].map((d) => ({ dia_semana: d, abre: null, cierra: null, todo_el_dia: true, notas: null }));
                }
                return diasMarcados().map((d) => ({ dia_semana: d, abre: campo("abre").value, cierra: campo("cierra").value, todo_el_dia: false, notas: null }));
            }

            Formularios.iniciar(form, {
                resumen: "resumen-errores",
                validar,
                alEnviar() {
                    const zona = window.DATOS.zonas.find((z) => z.id === campo("zona").value);
                    D.centros.push({
                        id: "n" + Date.now(), nombre: campo("nombre").value.trim(), tipo_centro_codigo: campo("tipo").value,
                        zona_id: zona.id, zona_nombre: zona.nombre,
                        direccion: campo("direccion").value.trim(), telefono_contacto: campo("telefono").value.trim(),
                        estado_verificacion: "pendiente", estado_operativo: "operativo", motivo_estado: null, demo_nuevo: true,
                        descripcion: campo("descripcion").value.trim() || null,
                        horarios: horariosDelForm(),
                        recursos_actualizado_at: new Date().toISOString(), recursos: {}
                    });
                    dialogo.close();
                    form.reset();
                    campo("horario-turno").hidden = false;
                    dibujar();
                    const aviso = campo("aviso-alta");
                    aviso.className = "alerta alerta-exito";
                    aviso.innerHTML = A.iconos.ok + "<p><strong>Centro de ejemplo agregado.</strong> No se envió ni se guardó ningún centro real. El ejemplo desaparece al recargar.</p>";
                }
            });

            campo("abrir-nuevo").addEventListener("click", () => { dialogo.showModal(); campo("nombre").focus(); });
            campo("cerrar-dialogo").addEventListener("click", () => dialogo.close());

            dibujar();
        })();

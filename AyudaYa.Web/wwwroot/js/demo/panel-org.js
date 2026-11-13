(function () {
            "use strict";
            const A = window.AyudaYa;
            const O = window.AyudaYaOrg;
            const D = window.DATOS_ORG;
            const org = D.organizacion;

            document.getElementById("titulo-org").textContent = org.nombre;
            document.getElementById("subtitulo-org").innerHTML =
                A.escapar(A.tiposOrganizacion[org.tipo]) + " · " + A.escapar(org.ciudad_nombre) + " · " + O.insigniaVerificacion(org.estado);

            const aviso = (clase, icono, html) => '<div class="alerta alerta-' + clase + '">' + icono + "<p>" + html + "</p></div>";
            const avisos = [];
            if (org.estado !== "activa") {
                avisos.push(aviso("advertencia", A.iconos.alerta, "<strong>La organización no está activa.</strong> Mientras tanto no puede tomar solicitudes."));
            }
            const pendientes = D.centros.filter((c) => c.estado_verificacion === "pendiente");
            const rechazados = D.centros.filter((c) => c.estado_verificacion === "rechazado");
            if (pendientes.length) {
                avisos.push(aviso("info", A.iconos.info, "<strong>" + O.plural(pendientes.length, "centro espera", "centros esperan") + " aprobación.</strong> Hasta que un administrador lo apruebe, no aparece en el mapa público."));
            }
            if (rechazados.length) {
                avisos.push(aviso("peligro", A.iconos.alerta, "<strong>" + O.plural(rechazados.length, "centro fue rechazado", "centros fueron rechazados") + ".</strong> Revisen el motivo en <a href=\"" + A.escapar(document.body.dataset.urlCentrosOrg) + "\">Centros</a>."));
            }
            document.getElementById("avisos").innerHTML = avisos.join("");

            const porFecha = (a, b) => new Date(b.asignada_at) - new Date(a.asignada_at);
            const activas = D.asignaciones.filter(O.esActiva).sort(porFecha);

            document.getElementById("n-activas").textContent = activas.length;
            document.getElementById("n-completadas").textContent = D.asignaciones.filter((a) => a.estado === "completada").length;
            document.getElementById("n-centros").textContent = D.centros.filter((c) => c.estado_verificacion === "aprobado").length;
            document.getElementById("n-miembros").textContent = D.miembros.filter((m) => m.activo).length;

            document.getElementById("lista-activas").innerHTML = activas.slice(0, 3).map((a) => "<li>" + O.tarjetaAsignacion(a) + "</li>").join("");
            document.getElementById("vacio").hidden = activas.length > 0;

            document.getElementById("lista-centros-panel").innerHTML = D.centros.map((c) =>
                '<li style="margin-bottom:.75rem"><a href="' + A.escapar(document.body.dataset.urlRecursosOrg) + '?id=' + encodeURIComponent(c.id) + '"><strong>' + A.escapar(c.nombre) + "</strong></a><br>" +
                O.insigniaVerificacion(c.estado_verificacion) + "</li>"
            ).join("");
        })();

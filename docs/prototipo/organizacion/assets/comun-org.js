// Funciones compartidas por las pantallas de la zona Responsable de organización.
// Va después de comun.js y reutiliza window.AyudaYa. También arma el
// encabezado y el pie, que son iguales en todas las pantallas de la zona.
(function () {
    "use strict";

    const A = window.AyudaYa;
    const D = window.DATOS_ORG;

    const ic = (trazos) => '<svg viewBox="0 0 16 16" aria-hidden="true" focusable="false">' + trazos + "</svg>";
    const trazo = 'fill="none" stroke="currentColor" stroke-width="1.6"';

    // Cada estado tiene ícono propio: no se distingue solo por color.
    const iconos = {
        reloj: ic('<circle cx="8" cy="8" r="6.5" ' + trazo + '/><path d="M8 4.5V8l2.5 1.5" ' + trazo + ' stroke-linecap="round"/>'),
        persona: ic('<circle cx="8" cy="5" r="2.6" ' + trazo + '/><path d="M2.8 14c.4-3 2.5-4.6 5.2-4.6s4.8 1.6 5.2 4.6" ' + trazo + ' stroke-linecap="round"/>'),
        corazon: ic('<path d="M8 13.5S2 10 2 6a3 3 0 0 1 6-1 3 3 0 0 1 6 1c0 4-6 7.5-6 7.5z" ' + trazo + ' stroke-linejoin="round"/>'),
        flecha: ic('<path d="M3 8h9M8.5 4l4 4-4 4" ' + trazo + ' stroke-linecap="round" stroke-linejoin="round"/>'),
        candado: ic('<rect x="3" y="7" width="10" height="7" rx="1.5" ' + trazo + '/><path d="M5.2 7V5a2.8 2.8 0 0 1 5.6 0v2" ' + trazo + '/>')
    };

    // Valores de asignacion.estado.
    const estadosAsignacion = {
        asignada: { texto: "Asignada", icono: iconos.reloj, descripcion: "Se comprometió el recurso, pero la atención todavía no empezó." },
        en_atencion: { texto: "En atención", icono: iconos.corazon, descripcion: "La atención está en curso." },
        completada: { texto: "Completada", icono: A.iconos.check, descripcion: "Terminaron la atención. Falta que la persona que pidió ayuda confirme." },
        cancelada: { texto: "Cancelada", icono: A.iconos.cruz, descripcion: "La asignación se canceló." },
        liberada: { texto: "Liberada", icono: iconos.flecha, descripcion: "La organización se retiró de esta atención." }
    };
    const activas = ["asignada", "en_atencion", "completada"];
    const esActiva = (a) => activas.includes(a.estado);

    // Valores de centro.estado_verificacion y organizacion.estado.
    const verificacion = {
        aprobado: { texto: "Aprobado", clase: "ver-aprobado", icono: A.iconos.check },
        activa: { texto: "Activa", clase: "ver-aprobado", icono: A.iconos.check },
        pendiente: { texto: "Pendiente de aprobación", clase: "ver-pendiente", icono: iconos.reloj },
        rechazado: { texto: "Rechazado", clase: "ver-rechazado", icono: A.iconos.cruz },
        rechazada: { texto: "Rechazada", clase: "ver-rechazado", icono: A.iconos.cruz },
        suspendido: { texto: "Suspendido", clase: "ver-rechazado", icono: A.iconos.pausa },
        suspendida: { texto: "Suspendida", clase: "ver-rechazado", icono: A.iconos.pausa }
    };

    function insigniaAsignacion(estado) {
        const e = estadosAsignacion[estado];
        return '<span class="insignia asig-' + estado + '">' + e.icono + '<span class="visualmente-oculto">Estado: </span>' + e.texto + "</span>";
    }

    function insigniaVerificacion(estado) {
        const v = verificacion[estado];
        return '<span class="insignia ' + v.clase + '">' + v.icono + '<span class="visualmente-oculto">Verificación: </span>' + v.texto + "</span>";
    }

    function insigniaUrgencia(urgencia) {
        return urgencia === "critica"
            ? '<span class="insignia urgencia-critica">' + A.iconos.exclamacion + '<span class="visualmente-oculto">Urgencia: </span>Crítica</span>'
            : '<span class="insignia urgencia-normal">' + A.iconos.guion + '<span class="visualmente-oculto">Urgencia: </span>Normal</span>';
    }

    const plural = (n, uno, varios) => n + " " + (n === 1 ? uno : varios);

    const DIAS = ["lun", "mar", "mié", "jue", "vie", "sáb", "dom"];
    const hora = (h) => h.slice(0, 5);

    // Resume las filas de centro_horario: "lun a vie 09:00–20:00 · sáb 09:00–13:00".
    // Agrupa días consecutivos con el mismo turno.
    function textoHorario(horarios) {
        if (!horarios || !horarios.length) return "Horario no informado";
        if (horarios.filter((h) => h.todo_el_dia).length === 7) return "Todos los días, las 24 horas";
        const clave = (h) => (h.todo_el_dia ? "24 horas" : hora(h.abre) + "–" + hora(h.cierra));
        const ordenadas = horarios.slice().sort((a, b) => a.dia_semana - b.dia_semana || (a.abre || "").localeCompare(b.abre || ""));
        const grupos = [];
        ordenadas.forEach((h) => {
            const g = grupos.find((x) => x.clave === clave(h) && x.fin === h.dia_semana - 1);
            if (g) g.fin = h.dia_semana;
            else grupos.push({ clave: clave(h), inicio: h.dia_semana, fin: h.dia_semana });
        });
        return grupos.map((g) => {
            const dias = g.inicio === g.fin ? DIAS[g.inicio - 1] : DIAS[g.inicio - 1] + (g.fin === g.inicio + 1 ? " y " : " a ") + DIAS[g.fin - 1];
            return dias + " " + g.clave;
        }).join(" · ");
    }

    function tarjetaAsignacion(a) {
        return '<article class="tarjeta solicitud" aria-labelledby="titulo-' + a.id + '">' +
            '<div class="cabeza">' +
                '<h3 id="titulo-' + a.id + '"><a href="asignacion.html?id=' + a.id + '">' + A.escapar(A.nombreRecurso(a.tipo_recurso)) + " · Solicitud #" + a.numero + "</a></h3>" +
                '<div class="insignias">' + insigniaAsignacion(a.estado) + (a.urgencia === "critica" && esActiva(a) ? insigniaUrgencia("critica") : "") + "</div>" +
            "</div>" +
            '<p class="meta">' + A.escapar(a.zona_nombre) + " · " + plural(a.personas, "persona", "personas") +
                " · asignada <time datetime=\"" + a.asignada_at + '" title="' + A.fechaCompleta(a.asignada_at) + '">' + A.haceCuanto(a.asignada_at) + "</time></p>" +
            (a.detalle ? '<p class="descripcion">' + A.escapar(a.detalle) + "</p>" : "") +
            '<div class="pie-solicitud"><span>Asignó ' + A.escapar(a.asignada_por) + "</span>" +
                '<a href="asignacion.html?id=' + a.id + '">Ver detalle<span class="visualmente-oculto"> de la asignación de ' + A.escapar(A.nombreRecurso(a.tipo_recurso)) + ", solicitud " + a.numero + "</span></a></div>" +
        "</article>";
    }

    const enlaces = [
        { href: "panel.html", texto: "Panel" },
        { href: "asignaciones.html", texto: "Asignaciones" },
        { href: "centros.html", texto: "Centros" },
        { href: "miembros.html", texto: "Miembros" }
    ];

    // Encabezado y pie. La pantalla actual se toma del nombre del archivo;
    // Detalle de asignación cuelga de Asignaciones y Recursos del centro, de Centros.
    function armarMarco() {
        const archivo = location.pathname.split("/").pop() || "panel.html";
        const padre = { "asignacion.html": "asignaciones.html", "recursos.html": "centros.html" };
        const actual = padre[archivo] || archivo;

        const items = enlaces.map((e) =>
            '<li><a href="' + e.href + '"' + (e.href === actual ? ' aria-current="page"' : "") + ">" + e.texto + "</a></li>"
        ).join("");

        document.body.insertAdjacentHTML("afterbegin",
            '<a class="saltar" href="#contenido">Saltar al contenido</a>' +
            '<div class="aviso-prototipo" role="note">Prototipo navegable · Zona responsable de organización · Datos ficticios</div>' +
            '<header class="encabezado"><div class="contenedor">' +
                '<a class="marca" href="panel.html" aria-label="AyudaYa, ir al panel de la organización">' +
                    '<svg viewBox="0 0 40 40" aria-hidden="true" focusable="false"><path d="M20 2C12.3 2 6 8.1 6 15.7 6 26 20 38 20 38s14-12 14-22.3C34 8.1 27.7 2 20 2z" fill="#0F766E"/><path d="M20 23.5s-6.5-4-6.5-8.3a3.6 3.6 0 0 1 6.5-2.1 3.6 3.6 0 0 1 6.5 2.1c0 4.3-6.5 8.3-6.5 8.3z" fill="#fff"/></svg>' +
                    '<span class="marca-texto">Ayuda<em>Ya</em></span></a>' +
                '<button type="button" class="boton-menu" aria-expanded="false" aria-controls="nav-principal">' +
                    '<svg width="20" height="20" viewBox="0 0 20 20" aria-hidden="true" focusable="false"><path d="M3 5h14M3 10h14M3 15h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>Menú</button>' +
                '<nav id="nav-principal" class="nav-principal" aria-label="Principal"><ul>' + items +
                    '<li><a href="../usuario/panel.html">Mi cuenta</a></li>' +
                    '<li><a href="../visitante/index.html">Cerrar sesión</a></li>' +
                "</ul></nav></div></header>");

        document.body.insertAdjacentHTML("beforeend",
            '<footer class="pie"><div class="contenedor">' +
                "<p>AyudaYa · TFI UTN FRGP 2026 · Equipo 46 NexusTech</p>" +
                '<p>Si hay riesgo de vida, llamá al <a href="tel:911">911</a>.</p></div></footer>');
    }

    armarMarco();

    window.AyudaYaOrg = {
        iconos, estadosAsignacion, activas, esActiva, verificacion,
        insigniaAsignacion, insigniaVerificacion, insigniaUrgencia, tarjetaAsignacion, plural, textoHorario, DIAS,
        usuario: D.usuario, organizacion: D.organizacion
    };
})();

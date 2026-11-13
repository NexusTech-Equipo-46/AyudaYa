// Datos ficticios de la zona Voluntario.
// La forma sigue las tablas voluntario, asignacion, solicitud_recurso y solicitud de
// database/AyudaYa_esquema_completo.sql. Una asignación es POR RECURSO pedido (no por
// solicitud completa) y tiene los estados del CHECK de la base:
// asignada, en_atencion, completada, cancelada y liberada. "Confirmada" no es un estado
// aparte: es una asignación completada con confirmada_at cargado (lo confirma quien pidió ayuda).
//
// Es el mismo voluntario que aparece en la zona Usuario ("Lucas P.", solicitud #1003).
// Personas, direcciones y teléfonos son inventados.
//
// Para probar:  ?demo=vacio (sin asignaciones)  ?demo=pausa (voluntario en pausa)
//               ?demo=reiniciar (borra lo que hiciste y vuelve a los datos de ejemplo)
// Lo que hacés (tomar, iniciar, completar, liberar) se guarda en sessionStorage mientras la
// pestaña esté abierta, así las tres pantallas se ven coherentes entre sí.
(function () {
    "use strict";

    const { ciudad, zonas } = window.DATOS;
    const ahora = Date.now();
    const haceHoras = (h) => new Date(ahora - h * 3600 * 1000).toISOString();
    const zonaNombre = (id) => zonas.find((z) => z.id === id).nombre;

    const demo = new URLSearchParams(location.search).get("demo");
    const persistir = demo !== "vacio" && demo !== "pausa";
    const CLAVE = "ayudaya-voluntario-v1";

    const voluntario = {
        nombre: "Lucas Pérez",
        email: "lucas.perez@ejemplo.com",
        ciudad_id: ciudad.id,
        ciudad_nombre: ciudad.nombre,
        zona_id: "z-02",
        zona_nombre: zonaNombre("z-02"),
        telefono_contacto: "011 4444-5555"
    };

    function asig(o) {
        return Object.assign({
            iniciada_at: null, completada_at: null, confirmada_at: null,
            cancelada_at: null, cancelada_por_nombre: null, motivo_cancelacion: null,
            liberada_at: null, motivo_liberacion: null
        }, o);
    }

    function sol(id, numero, urgencia, zona_id, personas, descripcion, creadoHaceH) {
        return { id, numero, urgencia, zona_id, zona_nombre: zonaNombre(zona_id), ciudad_nombre: ciudad.nombre,
            personas_afectadas: personas, descripcion, creado_at: haceHoras(creadoHaceH) };
    }

    const semilla = [
        asig({
            id: "a-2001", estado: "asignada", asignada_at: haceHoras(20),
            solicitud: sol("s-1003", "1003", "normal", "z-02", 3, "Nos quedamos sin mercadería después de la inundación.", 30),
            solicitante_nombre: "Marina Gómez",
            recurso: { tipo_recurso: "higiene", personas: 3, detalle: null },
            contacto: { direccion: "Calle Las Heras 455, El Talar", telefono: "011 5555-0123" }
        }),
        asig({
            id: "a-2002", estado: "en_atencion", asignada_at: haceHoras(6), iniciada_at: haceHoras(3),
            solicitud: sol("s-1010", "1010", "critica", "z-02", 6, "Familia con una persona electrodependiente. Sin agua potable desde ayer.", 8),
            solicitante_nombre: "Rosa Benítez",
            recurso: { tipo_recurso: "agua", personas: 6, detalle: "Bidones de 20 litros si hay" },
            contacto: { direccion: "Pasaje Los Álamos 77, El Talar", telefono: "011 5555-0456" }
        }),
        asig({
            id: "a-2003", estado: "completada", asignada_at: haceHoras(12), iniciada_at: haceHoras(9), completada_at: haceHoras(5),
            solicitud: sol("s-1008", "1008", "normal", "z-02", 2, null, 14),
            solicitante_nombre: "Julián Acosta",
            recurso: { tipo_recurso: "abrigo", personas: 2, detalle: "Frazadas y ropa de abrigo" },
            contacto: { direccion: "Av. Constituyentes 1880, El Talar", telefono: "011 5555-0789" }
        }),
        asig({
            id: "a-2004", estado: "completada", asignada_at: haceHoras(120), iniciada_at: haceHoras(118),
            completada_at: haceHoras(100), confirmada_at: haceHoras(70),
            solicitud: sol("s-1005", "1005", "normal", "z-02", 4, "Cuatro personas, entre ellas dos chicos.", 125),
            solicitante_nombre: "Silvia Romero",
            recurso: { tipo_recurso: "alimentos", personas: 4, detalle: null },
            contacto: { direccion: "Calle Belgrano 905, El Talar", telefono: "011 5555-0210" }
        }),
        asig({
            id: "a-2005", estado: "liberada", asignada_at: haceHoras(110), liberada_at: haceHoras(100),
            motivo_liberacion: "No pude conseguir la medicación. Conviene que la tome alguien con acceso a una farmacia.",
            solicitud: sol("s-1006", "1006", "normal", "z-02", 1, null, 112),
            solicitante_nombre: "Héctor Molina",
            recurso: { tipo_recurso: "medicamentos", personas: 1, detalle: "Medicación para la presión" },
            contacto: { direccion: "Calle Mitre 66, El Talar", telefono: "011 5555-0321" }
        }),
        asig({
            id: "a-2006", estado: "cancelada", asignada_at: haceHoras(160), cancelada_at: haceHoras(150),
            cancelada_por_nombre: "Paula Díaz", motivo_cancelacion: "Ya conseguí agua por mi cuenta.",
            solicitud: sol("s-1007", "1007", "normal", "z-02", 2, null, 162),
            solicitante_nombre: "Paula Díaz",
            recurso: { tipo_recurso: "agua", personas: 2, detalle: null },
            contacto: { direccion: "Calle Sáenz Peña 12, El Talar", telefono: "011 5555-0654" }
        })
    ];

    // Forma de v_solicitudes_abiertas. La dirección y el teléfono NO vienen en esa vista:
    // acá se guardan aparte (privado) solo para armar el detalle cuando el voluntario toma
    // el recurso. En la aplicación real los devuelve el servidor recién al tomar la asignación.
    const disponibles = [
        {
            id: "s-1012", numero: "1012", urgencia: "critica", estado: "pendiente",
            zona_id: "z-02", zona_nombre: zonaNombre("z-02"), ciudad_nombre: ciudad.nombre, personas_afectadas: 5,
            descripcion: "Casa inundada. Necesitan dónde pasar la noche, hay dos chicos.", creado_at: haceHoras(1),
            recursos: [
                { tipo_recurso: "alojamiento", personas: 5, detalle: null, estado: "pendiente" },
                { tipo_recurso: "abrigo", personas: 5, detalle: null, estado: "pendiente" }
            ],
            _privado: { solicitante_nombre: "Gabriela Sosa", direccion: "Calle Las Heras 120, El Talar", telefono: "011 5555-0912" }
        },
        {
            id: "s-1013", numero: "1013", urgencia: "normal", estado: "parcial",
            zona_id: "z-02", zona_nombre: zonaNombre("z-02"), ciudad_nombre: ciudad.nombre, personas_afectadas: 2,
            descripcion: "Adulto mayor que vive solo. Necesita alimentos para la semana.", creado_at: haceHoras(7),
            recursos: [
                { tipo_recurso: "alimentos", personas: 2, detalle: "Sin sal, es hipertenso", estado: "pendiente" },
                { tipo_recurso: "higiene", personas: 2, detalle: null, estado: "asignada" }
            ],
            _privado: { solicitante_nombre: "Oscar Vera", direccion: "Calle Belgrano 340, El Talar", telefono: "011 5555-0345" }
        },
        {
            id: "s-1014", numero: "1014", urgencia: "normal", estado: "pendiente",
            zona_id: "z-01", zona_nombre: zonaNombre("z-01"), ciudad_nombre: ciudad.nombre, personas_afectadas: 3,
            descripcion: "Sin luz ni gas desde hace dos días.", creado_at: haceHoras(12),
            recursos: [
                { tipo_recurso: "higiene", personas: 3, detalle: null, estado: "pendiente" },
                { tipo_recurso: "medicamentos", personas: 1, detalle: "Insulina", estado: "pendiente" }
            ],
            _privado: { solicitante_nombre: "Mariela Quiroga", direccion: "Calle Los Ceibos 88, General Pacheco", telefono: "011 5555-0678" }
        },
        {
            id: "s-1015", numero: "1015", urgencia: "critica", estado: "pendiente",
            zona_id: "z-03", zona_nombre: zonaNombre("z-03"), ciudad_nombre: ciudad.nombre, personas_afectadas: 4,
            descripcion: "Necesitan salir de la zona: el agua sigue subiendo.", creado_at: haceHoras(3),
            recursos: [
                { tipo_recurso: "traslado", personas: 4, detalle: null, estado: "pendiente" }
            ],
            _privado: { solicitante_nombre: "Federico Luna", direccion: "Calle Sarmiento 15, Benavídez", telefono: "011 5555-0777" }
        }
    ];

    // --- Estado de la sesión de prueba ---
    if (demo === "reiniciar") {
        try { sessionStorage.removeItem(CLAVE); } catch (e) { /* sin almacenamiento */ }
    }

    let estado = null;
    if (persistir) {
        try { estado = JSON.parse(sessionStorage.getItem(CLAVE)); } catch (e) { estado = null; }
    }
    if (!estado) {
        estado = {
            activo: demo !== "pausa",
            asignaciones: demo === "vacio" ? [] : semilla,
            tomados: []   // "idSolicitud|tipo_recurso" de lo que ya tomó en esta sesión
        };
    }

    function guardar() {
        if (!persistir) return;
        try { sessionStorage.setItem(CLAVE, JSON.stringify(estado)); } catch (e) { /* sin almacenamiento */ }
    }

    window.DATOS_VOLUNTARIO = { voluntario, estado, disponibles, guardar, demo, persistir };
})();

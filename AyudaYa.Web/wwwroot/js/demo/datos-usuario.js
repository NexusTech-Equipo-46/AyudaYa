// Datos ficticios de la zona Usuario autenticado.
// La forma sigue las tablas solicitud, solicitud_contacto, solicitud_recurso,
// asignacion e historial_solicitud de database/AyudaYa_esquema_completo.sql.
// Los estados son los del CHECK de la base. Personas, direcciones y teléfonos
// son inventados.
//
// Estados para probar las pantallas: s-1004 pendiente, s-1003 en atención,
// s-1002 parcial, s-1001 resuelta, s-0999 cancelada.
// Con ?demo=vacio en Panel o Mis solicitudes se ve el estado sin solicitudes.
(function () {
    "use strict";

    const ahora = Date.now();
    const haceHoras = (h) => new Date(ahora - h * 3600 * 1000).toISOString();
    const vacio = new URLSearchParams(location.search).get("demo") === "vacio";

    const usuario = {
        nombre: "Marina Gómez",
        email: "marina.gomez@ejemplo.com",
        dni: "30123456",
        voluntario: null   // null = todavía no completó "Quiero ser voluntario"
    };

    const orgManosUnidas = { tipo: "organizacion", nombre: "Asociación Manos Unidas" };

    const todas = [
        {
            id: "s-1004", numero: "1004",
            estado: "pendiente", urgencia: "critica",
            zona_nombre: "General Pacheco", ciudad_nombre: "Tigre",
            personas_afectadas: 4,
            descripcion: "Casa anegada y sin luz. Hay una persona mayor y un bebé de 8 meses.",
            motivo_cancelacion: null,
            creado_at: haceHoras(2), actualizado_at: haceHoras(2),
            contacto: { direccion: "Calle Los Ceibos 123, General Pacheco", telefono: "011 5555-0123" },
            recursos: [
                { tipo_recurso: "alojamiento", personas: 4, detalle: "Necesitamos una cama para la persona mayor", estado: "pendiente", asignado: null },
                { tipo_recurso: "abrigo", personas: 4, detalle: null, estado: "pendiente", asignado: null }
            ],
            historial: [
                { estado_anterior: null, estado_nuevo: "pendiente", actor: "Vos", motivo: null, creado_at: haceHoras(2) }
            ]
        },
        {
            id: "s-1003", numero: "1003",
            estado: "en_atencion", urgencia: "normal",
            zona_nombre: "El Talar", ciudad_nombre: "Tigre",
            personas_afectadas: 3,
            descripcion: "Nos quedamos sin mercadería después de la inundación.",
            motivo_cancelacion: null,
            creado_at: haceHoras(30), actualizado_at: haceHoras(5),
            contacto: { direccion: "Calle Las Heras 455, El Talar", telefono: "011 5555-0123" },
            recursos: [
                { tipo_recurso: "alimentos", personas: 3, detalle: "Una persona es celíaca", estado: "en_atencion", asignado: orgManosUnidas },
                { tipo_recurso: "higiene", personas: 3, detalle: null, estado: "asignada", asignado: { tipo: "voluntario", nombre: "Lucas P." } }
            ],
            historial: [
                { estado_anterior: null, estado_nuevo: "pendiente", actor: "Vos", motivo: null, creado_at: haceHoras(30) },
                { estado_anterior: "pendiente", estado_nuevo: "asignada", actor: "Asociación Manos Unidas", motivo: null, creado_at: haceHoras(26) },
                { estado_anterior: "asignada", estado_nuevo: "en_atencion", actor: "Asociación Manos Unidas", motivo: null, creado_at: haceHoras(5) }
            ]
        },
        {
            id: "s-1002", numero: "1002",
            estado: "parcial", urgencia: "normal",
            zona_nombre: "Benavídez", ciudad_nombre: "Tigre",
            personas_afectadas: 2,
            descripcion: null,
            motivo_cancelacion: null,
            creado_at: haceHoras(90), actualizado_at: haceHoras(48),
            contacto: { direccion: "Calle Sarmiento 98, Benavídez", telefono: "011 5555-0123" },
            recursos: [
                { tipo_recurso: "abrigo", personas: 2, detalle: "Frazadas y ropa de abrigo", estado: "resuelta", asignado: { tipo: "voluntario", nombre: "Camila R." } },
                { tipo_recurso: "medicamentos", personas: 1, detalle: "Medicación para la presión", estado: "pendiente", asignado: null }
            ],
            historial: [
                { estado_anterior: null, estado_nuevo: "pendiente", actor: "Vos", motivo: null, creado_at: haceHoras(90) },
                { estado_anterior: "pendiente", estado_nuevo: "asignada", actor: "Camila R.", motivo: null, creado_at: haceHoras(80) },
                { estado_anterior: "asignada", estado_nuevo: "en_atencion", actor: "Camila R.", motivo: null, creado_at: haceHoras(70) },
                { estado_anterior: "en_atencion", estado_nuevo: "parcial", actor: "Camila R.", motivo: "Se entregó el abrigo. Falta conseguir la medicación.", creado_at: haceHoras(48) }
            ]
        },
        {
            id: "s-1001", numero: "1001",
            estado: "resuelta", urgencia: "normal",
            zona_nombre: "Don Torcuato", ciudad_nombre: "Tigre",
            personas_afectadas: 5,
            descripcion: "Familia de cinco con los chicos en la escuela primaria.",
            motivo_cancelacion: null,
            creado_at: haceHoras(200), actualizado_at: haceHoras(170),
            contacto: { direccion: "Calle Ricardo Rojas 640, Don Torcuato", telefono: "011 5555-0123" },
            recursos: [
                { tipo_recurso: "alimentos", personas: 5, detalle: null, estado: "resuelta", asignado: orgManosUnidas }
            ],
            historial: [
                { estado_anterior: null, estado_nuevo: "pendiente", actor: "Vos", motivo: null, creado_at: haceHoras(200) },
                { estado_anterior: "pendiente", estado_nuevo: "asignada", actor: "Asociación Manos Unidas", motivo: null, creado_at: haceHoras(190) },
                { estado_anterior: "asignada", estado_nuevo: "en_atencion", actor: "Asociación Manos Unidas", motivo: null, creado_at: haceHoras(180) },
                { estado_anterior: "en_atencion", estado_nuevo: "resuelta", actor: "Asociación Manos Unidas", motivo: null, creado_at: haceHoras(170) }
            ]
        },
        {
            id: "s-0999", numero: "0999",
            estado: "cancelada", urgencia: "normal",
            zona_nombre: "Tigre Centro", ciudad_nombre: "Tigre",
            personas_afectadas: 2,
            descripcion: null,
            motivo_cancelacion: "Ya conseguí agua por mi cuenta.",
            creado_at: haceHoras(400), actualizado_at: haceHoras(399),
            contacto: { direccion: "Calle Cazón 310, Tigre", telefono: "011 5555-0123" },
            recursos: [
                { tipo_recurso: "agua", personas: 2, detalle: null, estado: "cancelada", asignado: null }
            ],
            historial: [
                { estado_anterior: null, estado_nuevo: "pendiente", actor: "Vos", motivo: null, creado_at: haceHoras(400) },
                { estado_anterior: "pendiente", estado_nuevo: "cancelada", actor: "Vos", motivo: "Ya conseguí agua por mi cuenta.", creado_at: haceHoras(399) }
            ]
        }
    ];

    window.DATOS_USUARIO = { usuario, todas, solicitudes: vacio ? [] : todas, vacio };
})();

// Datos ficticios de la zona Responsable de organización.
// La forma sigue las tablas organizacion, organizacion_usuario, centro,
// centro_recurso, solicitud_recurso y asignacion de
// database/AyudaYa_esquema_completo.sql. Los estados son los de los CHECK de
// la base. Personas, direcciones y teléfonos son inventados.
//
// Para probar: la organización es comunitaria y está activa.
// Centros: uno aprobado, uno pendiente y uno rechazado (con motivo). El rechazado
// no tiene horarios cargados ("horario no informado").
// Asignaciones: a-301 asignada, a-302 en atención, a-303 completada,
// a-304 liberada, a-305 cancelada.
// Con ?demo=vacio en Panel o Asignaciones se ve el estado sin asignaciones.
(function () {
    "use strict";

    const ahora = Date.now();
    const haceHoras = (h) => new Date(ahora - h * 3600 * 1000).toISOString();
    const vacio = new URLSearchParams(location.search).get("demo") === "vacio";

    // centro_horario: una fila por día (1 = lunes ... 7 = domingo) y turno.
    // Sin filas, el horario figura como no informado.
    const turnos = (dias, abre, cierra, notas) => dias.map((d) => ({ dia_semana: d, abre, cierra, todo_el_dia: false, notas: notas || null }));

    const usuario = { id: "u-01", nombre: "Sofía Benítez", email: "sofia.benitez@ejemplo.com" };

    // organizacion.estado: pendiente, activa, rechazada o suspendida.
    const organizacion = {
        id: "o-01", nombre: "Red Solidaria Pacheco", tipo: "comunitaria", estado: "activa",
        motivo_estado: null,
        ciudad_nombre: "Tigre",
        email_contacto: "contacto@redsolidaria.example", telefono_contacto: "011 4000-1001",
        descripcion: "Vecinos de General Pacheco que coordinan refugio, abrigo y alimentos durante las emergencias.",
        aprobada_at: haceHoras(24 * 40)
    };

    // centro.estado_verificacion: pendiente, aprobado, rechazado o suspendido.
    // centro.estado_operativo: operativo, saturado o cerrado.
    // recursos = centro_recurso (si falta un recurso, el centro no lo ofrece).
    const centros = [
        {
            id: "a1f3", nombre: "Refugio Parroquia San José", tipo_centro_codigo: "refugio",
            zona_id: "z-01", zona_nombre: "General Pacheco",
            direccion: "Av. Hipólito Yrigoyen 1450, General Pacheco", telefono_contacto: "011 4000-1001",
            estado_verificacion: "aprobado", estado_operativo: "operativo", motivo_estado: null,
            descripcion: "Alojamiento transitorio para familias evacuadas. Recibimos donaciones de 9 a 20 h.",
            horarios: turnos([1, 2, 3, 4, 5], "09:00", "20:00").concat(turnos([6], "09:00", "13:00")),
            recursos_actualizado_at: haceHoras(2),
            recursos: { alojamiento: "media", abrigo: "alta", alimentos: "baja", higiene: "media" }
        },
        {
            id: "k2m4", nombre: "Comedor Red Solidaria", tipo_centro_codigo: "comedor",
            zona_id: "z-01", zona_nombre: "General Pacheco",
            direccion: "Calle Los Ceibos 780, General Pacheco", telefono_contacto: "011 4000-1011",
            estado_verificacion: "pendiente", estado_operativo: "operativo", motivo_estado: null,
            descripcion: "Viandas para el mediodía.",
            horarios: turnos([1, 2, 3, 4, 5], "12:00", "15:00", "Retiro de viandas hasta las 14:30."),
            recursos_actualizado_at: haceHoras(20),
            recursos: { alimentos: "media" }
        },
        {
            id: "p5q7", nombre: "Punto de acopio Plaza Pacheco", tipo_centro_codigo: "punto_donacion",
            zona_id: "z-01", zona_nombre: "General Pacheco",
            direccion: "Plaza principal, General Pacheco", telefono_contacto: "011 4000-1012",
            estado_verificacion: "rechazado", estado_operativo: "cerrado",
            motivo_estado: "La dirección no corresponde a un lugar cerrado. Indicá un salón o local con responsable.",
            descripcion: null,
            horarios: [],
            recursos_actualizado_at: haceHoras(96),
            recursos: {}
        }
    ];

    // organizacion_usuario: rol responsable o miembro; baja lógica con motivo.
    const miembros = [
        { id: "m-1", nombre: "Sofía Benítez", email: "sofia.benitez@ejemplo.com", rol: "responsable", activo: true, creado_at: haceHoras(24 * 40), motivo_baja: null },
        { id: "m-2", nombre: "Martín Acosta", email: "martin.acosta@ejemplo.com", rol: "responsable", activo: true, creado_at: haceHoras(24 * 30), motivo_baja: null },
        { id: "m-3", nombre: "Julieta Ríos", email: "julieta.rios@ejemplo.com", rol: "miembro", activo: true, creado_at: haceHoras(24 * 12), motivo_baja: null },
        { id: "m-4", nombre: "Hugo Paz", email: "hugo.paz@ejemplo.com", rol: "miembro", activo: false, creado_at: haceHoras(24 * 25), motivo_baja: "Se mudó de zona." }
    ];

    // asignacion.estado: asignada, en_atencion, completada, cancelada o liberada.
    // Cada asignación cubre un solicitud_recurso; el contacto se muestra solo
    // mientras la asignación está activa (asignada, en_atencion o completada).
    const todas = [
        {
            id: "a-301", numero: "1004", estado: "asignada", urgencia: "critica",
            solicitante: "Marina Gómez", zona_nombre: "General Pacheco",
            tipo_recurso: "alojamiento", personas: 4, detalle: "Necesitamos una cama para la persona mayor",
            descripcion: "Casa anegada y sin luz. Hay una persona mayor y un bebé de 8 meses.",
            contacto: { direccion: "Calle Los Ceibos 123, General Pacheco", telefono: "011 5555-0123" },
            asignada_por: "Sofía Benítez", asignada_at: haceHoras(1.5),
            motivo: null, historial: [
                { estado_nuevo: "asignada", actor: "Sofía Benítez", motivo: null, creado_at: haceHoras(1.5) }
            ]
        },
        {
            id: "a-302", numero: "1003", estado: "en_atencion", urgencia: "normal",
            solicitante: "Rubén Díaz", zona_nombre: "El Talar",
            tipo_recurso: "alimentos", personas: 3, detalle: "Una persona es celíaca",
            descripcion: "Nos quedamos sin mercadería después de la inundación.",
            contacto: { direccion: "Calle Las Heras 455, El Talar", telefono: "011 5555-0188" },
            asignada_por: "Martín Acosta", asignada_at: haceHoras(26),
            motivo: null, historial: [
                { estado_nuevo: "asignada", actor: "Martín Acosta", motivo: null, creado_at: haceHoras(26) },
                { estado_nuevo: "en_atencion", actor: "Martín Acosta", motivo: null, creado_at: haceHoras(5) }
            ]
        },
        {
            id: "a-303", numero: "1001", estado: "completada", urgencia: "normal",
            solicitante: "Elena Suárez", zona_nombre: "Don Torcuato",
            tipo_recurso: "abrigo", personas: 5, detalle: null,
            descripcion: "Familia de cinco con los chicos en la escuela primaria.",
            contacto: { direccion: "Calle Ricardo Rojas 640, Don Torcuato", telefono: "011 5555-0144" },
            asignada_por: "Sofía Benítez", asignada_at: haceHoras(50),
            motivo: null, historial: [
                { estado_nuevo: "asignada", actor: "Sofía Benítez", motivo: null, creado_at: haceHoras(50) },
                { estado_nuevo: "en_atencion", actor: "Julieta Ríos", motivo: null, creado_at: haceHoras(40) },
                { estado_nuevo: "completada", actor: "Julieta Ríos", motivo: "Se entregaron frazadas y ropa. Falta la confirmación de la familia.", creado_at: haceHoras(8) }
            ]
        },
        {
            id: "a-304", numero: "0998", estado: "liberada", urgencia: "normal",
            solicitante: "Graciela Núñez", zona_nombre: "Benavídez",
            tipo_recurso: "traslado", personas: 2, detalle: "Silla de ruedas",
            descripcion: null, contacto: null,
            asignada_por: "Martín Acosta", asignada_at: haceHoras(100),
            motivo: "No contamos con vehículo adaptado para silla de ruedas.", historial: [
                { estado_nuevo: "asignada", actor: "Martín Acosta", motivo: null, creado_at: haceHoras(100) },
                { estado_nuevo: "liberada", actor: "Martín Acosta", motivo: "No contamos con vehículo adaptado para silla de ruedas.", creado_at: haceHoras(96) }
            ]
        },
        {
            id: "a-305", numero: "0995", estado: "cancelada", urgencia: "normal",
            solicitante: "Pablo Ferreyra", zona_nombre: "Tigre Centro",
            tipo_recurso: "agua", personas: 6, detalle: null,
            descripcion: null, contacto: null,
            asignada_por: "Sofía Benítez", asignada_at: haceHoras(180),
            motivo: "La solicitud se canceló: el solicitante consiguió agua por su cuenta.", historial: [
                { estado_nuevo: "asignada", actor: "Sofía Benítez", motivo: null, creado_at: haceHoras(180) },
                { estado_nuevo: "cancelada", actor: "Administración", motivo: "La solicitud se canceló: el solicitante consiguió agua por su cuenta.", creado_at: haceHoras(170) }
            ]
        }
    ];

    window.DATOS_ORG = { usuario, organizacion, centros, miembros, todas, asignaciones: vacio ? [] : todas, vacio };
})();

// Datos ficticios para el prototipo.
// La forma de cada centro copia la vista v_centros_publicos de
// database/AyudaYa_esquema_completo.sql, así las pantallas ya muestran
// exactamente lo que la base va a poder devolver.
// Los catálogos (tipos de centro y de recurso) son los que siembra el SQL.
// Ciudad, zonas, organizaciones y centros son inventados.

(function () {
    "use strict";

    const ahora = Date.now();
    const haceHoras = (h) => new Date(ahora - h * 3600 * 1000).toISOString();

    const tiposCentro = [
        { codigo: "refugio", nombre: "Refugio" },
        { codigo: "centro_evacuacion", nombre: "Centro de evacuación" },
        { codigo: "comedor", nombre: "Comedor" },
        { codigo: "hospital", nombre: "Hospital" },
        { codigo: "salita", nombre: "Salita" },
        { codigo: "punto_donacion", nombre: "Punto de donación" },
        { codigo: "otro", nombre: "Otro" }
    ];

    const tiposRecurso = [
        { codigo: "agua", nombre: "Agua" },
        { codigo: "alimentos", nombre: "Alimentos" },
        { codigo: "abrigo", nombre: "Abrigo" },
        { codigo: "higiene", nombre: "Higiene" },
        { codigo: "medicamentos", nombre: "Medicamentos" },
        { codigo: "alojamiento", nombre: "Alojamiento" },
        { codigo: "traslado", nombre: "Traslado" },
        { codigo: "mano_obra", nombre: "Mano de obra" },
        { codigo: "otros", nombre: "Otros" }
    ];

    const ciudad = { id: "c-01", nombre: "Tigre", provincia: "Buenos Aires" };

    const zonas = [
        { id: "z-01", nombre: "General Pacheco" },
        { id: "z-02", nombre: "El Talar" },
        { id: "z-03", nombre: "Benavídez" },
        { id: "z-04", nombre: "Don Torcuato" },
        { id: "z-05", nombre: "Tigre Centro" }
    ];

    const nombreTipoCentro = (codigo) => tiposCentro.find((t) => t.codigo === codigo).nombre;
    const zona = (id) => zonas.find((z) => z.id === id);

    function centro(c) {
        const z = zona(c.zona_id);
        return Object.assign({
            tipo_centro_nombre: nombreTipoCentro(c.tipo_centro_codigo),
            ciudad_id: ciudad.id,
            ciudad_nombre: ciudad.nombre,
            zona_nombre: z.nombre,
            email_contacto: null,
            descripcion: null
        }, c);
    }

    const centros = [
        centro({
            id: "a1f3", nombre: "Refugio Parroquia San José",
            tipo_centro_codigo: "refugio",
            organizacion_id: "o-01", organizacion_nombre: "Red Solidaria Pacheco", organizacion_tipo: "comunitaria",
            zona_id: "z-01",
            direccion: "Av. Hipólito Yrigoyen 1450, General Pacheco",
            telefono_contacto: "011 4000-1001", email_contacto: "refugio@redsolidaria.example",
            latitud: -34.4588, longitud: -58.6365,
            estado_operativo: "operativo",
            descripcion: "Alojamiento transitorio para familias evacuadas. Se reciben donaciones de abrigo y artículos de higiene de 9 a 20 h.",
            centro_actualizado_at: haceHoras(30), recursos_actualizado_at: haceHoras(2),
            recursos: { alojamiento: "media", abrigo: "alta", alimentos: "baja", higiene: "media" }
        }),
        centro({
            id: "b2c4", nombre: "Comedor Los Abuelos",
            tipo_centro_codigo: "comedor",
            organizacion_id: "o-02", organizacion_nombre: "Comedor Comunitario Los Abuelos", organizacion_tipo: "comunitaria",
            zona_id: "z-02",
            direccion: "Calle Las Heras 820, El Talar",
            telefono_contacto: "011 4000-1002",
            latitud: -34.4715, longitud: -58.6542,
            estado_operativo: "saturado",
            descripcion: "Almuerzo y merienda todos los días. Hoy estamos al límite de capacidad: si podés, acercate a otro comedor de la zona.",
            centro_actualizado_at: haceHoras(5), recursos_actualizado_at: haceHoras(1),
            recursos: { alimentos: "baja", agua: "media" }
        }),
        centro({
            id: "c3d5", nombre: "Centro de evacuación Club El Talar",
            tipo_centro_codigo: "centro_evacuacion",
            organizacion_id: "o-03", organizacion_nombre: "Defensa Civil local", organizacion_tipo: "institucional",
            zona_id: "z-02",
            direccion: "Av. Constituyentes 2300, El Talar",
            telefono_contacto: "011 4000-1003", email_contacto: "evacuados@defensacivil.example",
            latitud: -34.4762, longitud: -58.6481,
            estado_operativo: "operativo",
            descripcion: "Gimnasio del club habilitado como centro de evacuación. Atención las 24 horas.",
            centro_actualizado_at: haceHoras(12), recursos_actualizado_at: haceHoras(4),
            recursos: { alojamiento: "alta", agua: "alta", alimentos: "media", abrigo: "media", higiene: "baja", medicamentos: "baja" }
        }),
        centro({
            id: "d4e6", nombre: "Hospital Zonal Norte",
            tipo_centro_codigo: "hospital",
            organizacion_id: "o-04", organizacion_nombre: "Hospital Zonal Norte", organizacion_tipo: "institucional",
            zona_id: "z-04",
            direccion: "Av. Ángel T. de Alvear 3100, Don Torcuato",
            telefono_contacto: "011 4000-1004",
            latitud: -34.4925, longitud: -58.6231,
            estado_operativo: "operativo",
            descripcion: "Guardia las 24 horas. Para emergencias médicas llamá al 107.",
            centro_actualizado_at: haceHoras(48), recursos_actualizado_at: haceHoras(6),
            recursos: { medicamentos: "media", traslado: "alta" }
        }),
        centro({
            id: "e5f7", nombre: "Salita Barrio Las Tunas",
            tipo_centro_codigo: "salita",
            organizacion_id: "o-05", organizacion_nombre: "Centros de Atención Primaria Tigre", organizacion_tipo: "institucional",
            zona_id: "z-01",
            direccion: "Calle Los Ceibos 560, General Pacheco",
            telefono_contacto: "011 4000-1005",
            latitud: -34.4502, longitud: -58.6268,
            estado_operativo: "operativo",
            descripcion: "Atención primaria de lunes a sábado de 8 a 18 h.",
            centro_actualizado_at: haceHoras(20), recursos_actualizado_at: haceHoras(9),
            recursos: { medicamentos: "baja", higiene: "media" }
        }),
        centro({
            id: "f6a8", nombre: "Punto de acopio Escuela Técnica 2",
            tipo_centro_codigo: "punto_donacion",
            organizacion_id: "o-06", organizacion_nombre: "Cooperadora Escuela Técnica 2", organizacion_tipo: "comunitaria",
            zona_id: "z-03",
            direccion: "Av. Benavídez 1230, Benavídez",
            telefono_contacto: "011 4000-1006",
            latitud: -34.4108, longitud: -58.6893,
            estado_operativo: "operativo",
            descripcion: "Recibimos y clasificamos donaciones. Lo más necesario hoy: abrigo para adultos.",
            centro_actualizado_at: haceHoras(10), recursos_actualizado_at: haceHoras(3),
            recursos: { agua: "alta", higiene: "alta", abrigo: "sin_stock", alimentos: "media" }
        }),
        centro({
            id: "a7b9", nombre: "Sociedad de Fomento Tigre Centro",
            tipo_centro_codigo: "punto_donacion",
            organizacion_id: "o-07", organizacion_nombre: "Sociedad de Fomento Tigre Centro", organizacion_tipo: "comunitaria",
            zona_id: "z-05",
            direccion: "Calle Cazón 940, Tigre",
            telefono_contacto: "011 4000-1007",
            latitud: -34.4249, longitud: -58.5803,
            estado_operativo: "cerrado",
            descripcion: "Cerrado temporalmente por anegamiento del salón.",
            centro_actualizado_at: haceHoras(80), recursos_actualizado_at: haceHoras(76),
            recursos: { alimentos: "baja", abrigo: "baja" }
        }),
        centro({
            id: "b8ca", nombre: "Comedor Manos Unidas",
            tipo_centro_codigo: "comedor",
            organizacion_id: "o-08", organizacion_nombre: "Asociación Manos Unidas", organizacion_tipo: "comunitaria",
            zona_id: "z-03",
            direccion: "Calle Sarmiento 410, Benavídez",
            telefono_contacto: "011 4000-1008",
            latitud: -34.4163, longitud: -58.6807,
            estado_operativo: "operativo",
            descripcion: "Viandas de lunes a viernes a partir de las 12 h.",
            centro_actualizado_at: haceHoras(3), recursos_actualizado_at: haceHoras(0.5),
            recursos: { alimentos: "alta", agua: "media" }
        }),
        centro({
            // Sin coordenadas: la base lo permite. Aparece en la lista pero no en el mapa.
            id: "c9db", nombre: "Bomberos Voluntarios Don Torcuato",
            tipo_centro_codigo: "otro",
            organizacion_id: "o-09", organizacion_nombre: "Asociación de Bomberos Voluntarios", organizacion_tipo: "institucional",
            zona_id: "z-04",
            direccion: "Calle Ricardo Rojas 1700, Don Torcuato",
            telefono_contacto: "011 4000-1009",
            latitud: null, longitud: null,
            estado_operativo: "operativo",
            descripcion: "Coordinamos traslados y cuadrillas de trabajo para limpieza.",
            centro_actualizado_at: haceHoras(26), recursos_actualizado_at: haceHoras(14),
            recursos: { traslado: "media", mano_obra: "alta" }
        })
    ];

    window.DATOS = { ciudad, zonas, tiposCentro, tiposRecurso, centros };
})();

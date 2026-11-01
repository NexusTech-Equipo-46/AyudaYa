# Prototipo de pantallas

Prototipo navegable de AyudaYa (issue #4), desarrollado en HTML, CSS y JavaScript, sin dependencias de compilación. Sigue el mapa de navegación de la bitácora y la paleta propuesta para la guía de estilo.

## Cómo verlo

Abrir con doble clic en cualquier navegador:

- Visitante: `visitante/index.html`
- Usuario autenticado: `usuario/panel.html`
- Responsable de organización: `organizacion/panel.html`

El mapa utiliza Leaflet y OpenStreetMap, por lo que requiere conexión a internet. El resto funciona sin conexión.

También se puede ejecutar desde la terminal:

```bash
python -m http.server 8765 --directory docs/prototipo
```

Luego ingresar a:
- `http://localhost:8765/visitante/`
- `http://localhost:8765/usuario/panel.html`
- `http://localhost:8765/organizacion/panel.html`

El resumen de la zona Visitante está en [visitante/AyudaYa-prototipo-visitante.pdf](visitante/AyudaYa-prototipo-visitante.pdf). Incluye versiones de escritorio y celular, con notas por pantalla. La zona Usuario autenticado todavía no tiene resumen en PDF.

## Estructura

Cada zona tiene su propia carpeta y puede abrirse de forma independiente.

```text
docs/prototipo/
├── visitante/
│   ├── index, login, registro, mapa, centro (.html)
│   └── assets/
│       ├── estilos.css
│       ├── comun.js
│       ├── datos.js
│       ├── formularios.js
│       ├── mapa.js
│       └── centro.js
└── usuario/
    ├── panel, mis-solicitudes, nueva-solicitud, solicitud,
    │   perfil, voluntariado, pendiente (.html)
    └── assets/
        ├── estilos-usuario.css
        ├── datos-usuario.js
        ├── comun-usuario.js
        ├── estilos.css
        ├── comun.js
        ├── datos.js
        └── formularios.js
└── organizacion/
    ├── panel, asignaciones, asignacion, centros, recursos, miembros (.html)
    └── assets/
        ├── estilos-org.css
        ├── datos-org.js
        ├── comun-org.js
        ├── estilos.css
        ├── estilos-usuario.css
        ├── comun.js
        ├── datos.js
        └── formularios.js
```

`usuario/assets/` contiene copias de los archivos base de `visitante/assets/` para que ambas zonas sean independientes. Si cambia la paleta o un componente común, hay que replicar el cambio (ver Decisión 9).

Los únicos enlaces desde `usuario/` hacia `visitante/` son **Mapa de centros** y **Cerrar sesión**.

## Avance por zona

Las zonas corresponden a las columnas del mapa de navegación.

| Zona | Pantallas | Estado |
|---|---|---|
| Visitante | Inicio, Mapa de centros, Detalle de centro, Iniciar sesión, Crear cuenta | Terminado |
| Usuario autenticado | Panel, Mis solicitudes, Nueva solicitud, Detalle de solicitud, Perfil, Quiero ser voluntario, Pendiente | Terminado |
| Voluntario | Panel, Mis asignaciones, Detalle de asignación | Pendiente |
| Responsable de organización | Panel, Asignaciones, Detalle de asignación, Centros, Recursos del centro, Miembros | Terminado |
| Administrador | Dashboard, Organizaciones, Centros, Usuarios, Catálogos, Métricas, Auditoría | Pendiente |

Para sumar una zona, copiar la estructura de `usuario/` (se pueden reutilizar los estilos base y `comun.js`) y agregarla a esta tabla.

## Zona Visitante

| Pantalla | Ruta en la app | Archivo |
|---|---|---|
| Inicio | `/` | `index.html` |
| Mapa de centros | `/mapa` | `mapa.html` |
| Detalle de centro | `/centros/{id}` | `centro.html?id=…` |
| Iniciar sesión | `/account/login` | `login.html` |
| Crear cuenta | `/account/register` | `registro.html` |

**Recorridos para probar**

1. Inicio → «Alimentos» → mapa filtrado → centro → «Pedir ayuda» → iniciar sesión con aviso.
2. Mapa → zona «Tigre Centro» → Sociedad de Fomento (cerrado y con datos de hace 3 días).
3. Mapa → Bomberos Voluntarios (sin ubicación: aparece en la lista, pero no en el mapa).
4. Crear cuenta → enviar vacío → corregir errores desde el resumen.
5. Reducir la ventana a tamaño celular → filtros plegables y selector Lista / Mapa.

**Datos**

`visitante/assets/datos.js` contiene centros ficticios con la misma estructura que la vista `v_centros_publicos` de `database/AyudaYa_esquema_completo.sql`, y los catálogos `tipo_centro` y `tipo_recurso` que siembra ese script.

Ciudad, zonas, organizaciones, direcciones y teléfonos son inventados.

## Zona Usuario autenticado

| Pantalla | Ruta en la app | Archivo |
|---|---|---|
| Panel | `/panel` | `panel.html` |
| Mis solicitudes | `/solicitudes` (propuesta) | `mis-solicitudes.html` |
| Nueva solicitud | `/solicitudes/nueva` | `nueva-solicitud.html` |
| Pendiente (confirmación al enviar) | A definir | `pendiente.html` |
| Detalle de solicitud | `/solicitudes/{id}` (propuesta) | `solicitud.html?id=…` |
| Perfil | `/perfil` (propuesta) | `perfil.html` |
| Quiero ser voluntario | `/voluntariado` | `voluntariado.html` |

Las rutas de Panel, Nueva solicitud y Quiero ser voluntario ya aparecen en `login.html` y `registro.html`. Las marcadas como propuesta son inventadas y deben confirmarse en grupo.

**Recorridos para probar**

1. Panel → «Pedir ayuda» → enviar vacío → corregir errores desde el resumen → elegir recursos (cada uno solicita su cantidad de personas) → enviar → Pendiente → «Ver el detalle».
2. Mis solicitudes → filtro «Cerradas» (una resuelta y una cancelada) → abrir cada una.
3. Detalles s-1003 (en atención) y s-1002 (atención parcial): cada recurso muestra su estado y quién lo atiende.
4. Detalle s-1004 (pendiente) → «Cancelar solicitud» → confirmar sin motivo (error) → confirmar con motivo (pasa a cancelada y agrega una línea al historial).
5. Perfil → DNI de 2 dígitos (error) → cambiar contraseña viendo los requisitos en vivo.
6. Quiero ser voluntario → enviar vacío → completar → confirmación.
7. Panel o Mis solicitudes con `?demo=vacio` → estado sin solicitudes.
8. Reducir la ventana a tamaño celular → menú plegable y tablas con scroll horizontal.

**Datos**

`usuario/assets/datos-usuario.js` contiene un usuario ficticio y cinco solicitudes, una por cada estado posible:

- s-1004: pendiente.
- s-1003: en atención.
- s-1002: parcial.
- s-1001: resuelta.
- s-0999: cancelada.

Siguen la estructura de las tablas `solicitud`, `solicitud_contacto`, `solicitud_recurso`, `asignacion` e `historial_solicitud` de `database/AyudaYa_esquema_completo.sql`.

Los catálogos de zonas y tipos de recurso provienen de `usuario/assets/datos.js`.

## Zona Responsable de organización

| Pantalla | Ruta en la app | Archivo |
|---|---|---|
| Panel | `/organizacion` (propuesta) | `panel.html` |
| Asignaciones | `/organizacion/asignaciones` (propuesta) | `asignaciones.html` |
| Detalle de asignación | `/organizacion/asignaciones/{id}` (propuesta) | `asignacion.html?id=…` |
| Centros | `/organizacion/centros` (propuesta) | `centros.html` |
| Recursos del centro | `/organizacion/centros/{id}/recursos` (propuesta) | `recursos.html?id=…` |
| Miembros | `/organizacion/miembros` (propuesta) | `miembros.html` |

Todas las rutas son propuestas y hay que confirmarlas en grupo. El encabezado y el pie los arma `assets/comun-org.js`, así que no se repiten en cada página.

**Recorridos para probar**

1. Panel → avisos de centro pendiente y rechazado → «Ver detalle» de una asignación.
2. Asignaciones → filtros Activas, Cerradas y Todas.
3. Detalle de a-301 (asignada) → «Empezar la atención» → «Marcar como completada». Queda esperando la confirmación de la persona que pidió ayuda y ya no ofrece acciones.
4. Detalle de a-302 (en atención) → «Liberar asignación» → confirmar sin motivo (error) → confirmar con motivo (pasa a liberada y agrega una línea al historial).
5. Detalle de a-304 (liberada) y a-305 (cancelada): los datos de contacto ya no se muestran.
6. Centros → «Nuevo centro» → enviar vacío → corregir errores desde el resumen → el centro queda pendiente. Probar también un nombre repetido, marcar días sin horas, una hora de cierre anterior a la de apertura y la casilla de 24 horas.
7. Centros → «Recursos» del Refugio Parroquia San José → cambiar niveles (incluido «No ofrece») → guardar: se actualiza la fecha.
8. Miembros → dar de baja a Julieta (pide motivo) → dar de baja a uno de los dos responsables → intentar dar de baja al último: la regla de la base lo impide.
9. Panel o Asignaciones con `?demo=vacio` → estado sin asignaciones.
10. Reducir la ventana a tamaño celular → menú plegable y tablas con scroll horizontal.

**Datos**

`organizacion/assets/datos-org.js` contiene una organización comunitaria activa, tres centros (aprobado, pendiente y rechazado con motivo; el rechazado sin horario cargado), cuatro miembros y cinco asignaciones, una por cada estado:

- a-301: asignada.
- a-302: en atención.
- a-303: completada.
- a-304: liberada.
- a-305: cancelada.

Siguen la estructura de las tablas `organizacion`, `organizacion_usuario`, `centro`, `centro_horario`, `centro_recurso` y `asignacion` de `database/AyudaYa_esquema_completo.sql`. Los catálogos de zonas, tipos de centro y tipos de recurso provienen de `organizacion/assets/datos.js`.

## Qué es real y qué es simulado

- **Validaciones:** son las del prototipo y siguen las reglas de la base: DNI de 6 a 9 dígitos, personas mayores a cero y motivo obligatorio al cancelar.
- **Persistencia:** no se guarda información. Al recargar, una solicitud cancelada vuelve a su estado original.
- **Nueva solicitud:** pasa un resumen a Pendiente mediante `sessionStorage`. El botón «Ver el detalle» siempre abre s-1004 como ejemplo.
- **Editar solicitud:** solo muestra un aviso. En la aplicación real se reutilizará el formulario de Nueva solicitud.
- **Zona organización:** los cambios de estado, altas y bajas viven en memoria y se pierden al recargar. «Nuevo centro» agrega una tarjeta pendiente sin guardarla.
- **Sesión:** no está implementada. «Cerrar sesión» vuelve al inicio de `visitante/`.

## Accesibilidad aplicada

La zona Visitante utiliza fuente de 16 px, enlace «Saltar al contenido», foco visible, botones de al menos 44 px y formularios con resumen de errores que recibe el foco.

Además, la zona Usuario incluye:

- Estados de solicitudes y recursos con ícono y texto, nunca solo color. Cada estado tiene su propio ícono.
- Colores de las variables de la paleta. No se agregan colores nuevos, salvo un rojo más oscuro para el hover del botón de cancelar.
- Cancelación mediante `<dialog>` nativo: atrapa el foco, se cierra con Esc y devuelve el foco al botón que lo abrió.
- Urgencia organizada con `fieldset` y `legend`. Los recursos usan casillas que muestran sus campos al marcarlas.
- Tablas con encabezados de fila y columna, dentro de una región con scroll accesible por teclado.
- Historial como lista ordenada, con fechas dentro de `<time>`.

## Decisiones para revisar en el grupo

1. **Estados:** se usan los de la base (`pendiente`, `asignada`, `en_atencion`, `parcial`, `resuelta`, `cancelada`), no los del issue («en camino», «entregada»). La solicitud puede quedar parcial y cada recurso tiene su propio estado.

2. **Registro sin tipo de usuario:** el mapa de navegación indica que los roles no son exclusivos. Por eso, voluntariado y organización se agregan desde el panel. El issue #4 todavía menciona elegir el tipo al registrarse.

3. **Horario del centro:** el issue lo pide en el detalle, pero la tabla `centro` no tiene ese campo. Por ahora se incluye en la descripción.

4. **Contraseñas:** el prototipo exige 8 caracteres, mayúscula, minúscula y número. Hay que configurar las mismas reglas en Identity. También se aplican al cambio de contraseña desde Perfil.

5. **Logo:** el ícono del encabezado es provisorio hasta tener el logo final.

6. **Pantalla Pendiente:** el mapa de navegación la ubica en Usuario autenticado, pero no la describe. Se interpretó como la confirmación después de enviar una solicitud. Si representa otra situación (por ejemplo, una organización pendiente de aprobación), hay que rehacerla.

7. **Número de solicitud:** la tabla `solicitud` usa UUID como clave y no tiene un número legible. El prototipo muestra «Solicitud #1004». Hay que decidir si se agrega una columna secuencial o se muestra un código corto del UUID.

8. **Datos visibles para quien pidió ayuda:** el detalle muestra el nombre del voluntario u organización que atiende cada recurso. El historial muestra el nombre de quien cambió el estado. Hay que confirmar qué datos de quien asiste puede ver el solicitante.

9. **Carpeta usuario autocontenida:** contiene copias de los archivos base de `visitante/`. La alternativa es compartir un único `assets/` en la raíz. Conviene decidirlo antes de sumar las zonas pendientes.

10. **Voluntariado:** la zona es opcional (`voluntario.zona_id` acepta nulo). El teléfono se pide aunque la columna también lo permita, porque el aviso de «Crear cuenta» dice que se completan ciudad, zona y teléfono. La casilla de consentimiento para compartir nombre y teléfono no tiene una columna en la base: habría que agregarla o quitarla.

11. **Cancelación:** se permite mientras la solicitud esté abierta (`pendiente`, `asignada`, `en atención` o `parcial`) y siempre con motivo, como indica el `CHECK` de la tabla. La edición solo se ofrece en estado pendiente.

12. **Descripción pública:** `v_solicitudes_abiertas` expone la descripción y el detalle de cada recurso a otros usuarios autenticados. Por eso, el formulario advierte que no se debe incluir dirección ni teléfono.

## Decisiones para revisar: zona organización

13. **Horario del centro:** «Nuevo centro» ya usa `centro_horario` (PR #11): casilla de 24 horas (`todo_el_dia`) o días con hora de apertura y cierre, que se guardan como una fila por día. La base exige que la apertura sea anterior al cierre, así que un turno que cruza la medianoche (por ejemplo, de 20 a 6) no entra. El prototipo carga un solo turno por día aunque la tabla admite varios. Hay que decidir si alcanza para el MVP, si se permiten turnos partidos y qué hacer con el cruce de medianoche. Las notas por día (`notas`) todavía no tienen campo. El resumen se ve en cada tarjeta de Centros, por ejemplo «lun a vie 09:00–20:00 · sáb 09:00–13:00».

14. **Asignar desde la organización:** el prototipo muestra las asignaciones que ya tiene la organización, pero no cómo toma una solicitud. Falta definir si se elige desde la zona Voluntario o desde un listado de solicitudes abiertas propio de la organización (`v_solicitudes_abiertas`).

15. **Quién puede avanzar o liberar una asignación:** la base no valida quién inicia, completa o libera; eso es de Negocio. El prototipo permite hacerlo a cualquier persona activa de la organización. Hay que decidir si solo responsables o también miembros.

16. **Datos de contacto:** se muestran mientras la asignación está activa (asignada, en atención o completada) y se ocultan al cerrarse. Hay que confirmarlo con la regla de privacidad de `solicitud_contacto`.

17. **Registro de la organización:** el alta (`organizacion` queda pendiente hasta que la apruebe un administrador) no tiene pantalla en el mapa de navegación de esta zona. El panel solo contempla el aviso de «no activa». Hay que decidir si se agrega la pantalla o si queda en el panel del Usuario autenticado.

18. **Baja de miembros:** se aplica la regla de la base (no dejar una organización comunitaria sin responsable activo), pero el prototipo no maneja el cambio de rol. Faltaría una acción para pasar de miembro a responsable.

19. **Centros no aprobados:** pueden cargar recursos antes de que el centro se apruebe, con un aviso de que todavía no se ve en el mapa. Hay que confirmar si eso es lo que se quiere.

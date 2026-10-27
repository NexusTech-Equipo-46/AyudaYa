# Prototipo de pantallas

Prototipo navegable de AyudaYa (issue #4), armado en HTML, CSS y JavaScript
sin dependencias de compilación. Sigue el mapa de navegación publicado en la
bitácora y la paleta propuesta para la guía de estilo.

## Cómo verlo

Abrí `visitante/index.html` con doble clic en cualquier navegador.
El mapa usa Leaflet y OpenStreetMap desde internet, así que necesita conexión;
sin conexión el resto de las pantallas funciona igual.

Si preferís servirlo:

```bash
python -m http.server 8765 --directory docs/prototipo
```

y entrá a `http://localhost:8765/visitante/`.

El resumen para leer sin abrir el navegador está en
[`visitante/AyudaYa-prototipo-visitante.pdf`](visitante/AyudaYa-prototipo-visitante.pdf)
(escritorio y celular, con notas por pantalla).

## Avance por zona

Las zonas son las columnas del mapa de navegación.

| Zona | Pantallas | Estado |
|---|---|---|
| Visitante | Inicio, Mapa de centros, Detalle de centro, Iniciar sesión, Crear cuenta | ✅ `visitante/` |
| Usuario autenticado | Panel, Mis solicitudes, Nueva solicitud, Detalle de solicitud, Perfil, Quiero ser voluntario | Pendiente |
| Voluntario | Panel, Mis asignaciones, Detalle de asignación | Pendiente |
| Responsable de organización | Panel, Asignaciones, Detalle de asignación, Centros, Recursos del centro, Miembros | Pendiente |
| Administrador | Dashboard, Organizaciones, Centros, Usuarios, Catálogos, Métricas, Auditoría | Pendiente |

Para sumar una zona: copiá la estructura de `visitante/` (los estilos y
`comun.js` se pueden reutilizar tal cual) y agregá la fila acá.

## Zona Visitante

| Pantalla | Ruta en la app | Archivo |
|---|---|---|
| Inicio | `/` | `index.html` |
| Mapa de centros | `/mapa` | `mapa.html` |
| Detalle de centro | `/centros/{id}` | `centro.html?id=…` |
| Iniciar sesión | `/account/login` | `login.html` |
| Crear cuenta | `/account/register` | `registro.html` |

Recorridos para probar:

- Inicio → “Alimentos” → el mapa abre filtrado → un centro → “Pedir ayuda” → iniciar sesión con aviso.
- Mapa → zona “Tigre Centro” → *Sociedad de Fomento* (cerrado y con datos de hace 3 días).
- Mapa → *Bomberos Voluntarios* (sin ubicación: aparece en la lista y no en el mapa).
- Crear cuenta → enviar vacío → corregir los errores desde el resumen.
- Achicar la ventana a celular → filtros plegables y selector Lista / Mapa.

### Datos

`visitante/assets/datos.js` tiene centros ficticios con la misma forma que la
vista `v_centros_publicos` de `database/AyudaYa_esquema_completo.sql`, y los
catálogos `tipo_centro` y `tipo_recurso` que siembra ese script. Ciudad, zonas,
organizaciones, direcciones y teléfonos son inventados.

### Accesibilidad aplicada

- Texto base de 16 px y contraste de al menos 4.5:1 en todos los pares de texto
  (los bordes de controles, al menos 3:1).
- Disponibilidad y estado del centro con ícono y texto, nunca solo color.
- Enlace “Saltar al contenido”, foco visible, regiones y encabezados semánticos.
- La lista de centros tiene la misma información que el mapa; los marcadores
  se recorren con Tab y se abren con Enter.
- Formularios con etiquetas visibles, `autocomplete`, resumen de errores que
  recibe el foco y mensajes asociados a cada campo.
- Botones y enlaces de al menos 44 px de alto.

## Decisiones para revisar en el grupo

1. **Estados**: se usan los de la base (`pendiente`, `asignada`, `en_atencion`,
   `resuelta`, `cancelada`), no los del issue (*en camino*, *entregada*).
2. **Registro sin “tipo de usuario”**: el mapa de navegación dice que los roles
   no son exclusivos, así que voluntario y organización se agregan desde el
   panel. El issue #4 todavía menciona elegir el tipo al registrarse.
3. **Horario del centro**: el issue lo pide en el detalle, pero la tabla
   `centro` no tiene ese campo. Por ahora va dentro de la descripción.
4. **Reglas de contraseña**: el prototipo pide 8 caracteres con mayúscula,
   minúscula y número; hay que configurarlo igual en Identity.
5. **Logo**: el ícono del encabezado es provisorio hasta tener el logo final.

# AyudaYa

Plataforma web para coordinar ayuda en emergencias (inundaciones, temporales, etc.): las personas afectadas piden ayuda, los voluntarios y las organizaciones la toman y la atienden, y cualquiera puede ver en un mapa qué centros (refugios, comedores, salitas, puntos de donación) están abiertos y con qué recursos.

TFI UTN FRGP 2026 · Equipo 46 NexusTech.

## Qué hace

| Quién | Qué puede hacer |
|---|---|
| **Visitante** | Ver el mapa de centros con filtros (zona, tipo, recurso), el detalle de cada centro, las zonas con pedidos abiertos (aproximado) y el impacto de la red. |
| **Persona con cuenta** | Pedir ayuda (recursos, urgencia, ubicación exacta en el mapa), seguir y cancelar sus solicitudes, **confirmar** que recibió cada recurso. |
| **Voluntario** | Inscribirse con ciudad/zona, ver solicitudes disponibles, tomar un recurso, iniciarlo, completarlo o liberarlo; pausar su disponibilidad. |
| **Organización** | Registrarse (queda pendiente de aprobación), cargar centros y horarios, actualizar la disponibilidad de recursos, tomar solicitudes y operar sus asignaciones, administrar su equipo. |
| **Administrador** | Gestionar ciudades y zonas, aprobar o suspender organizaciones y centros, desactivar usuarios, cancelar solicitudes y consultar la auditoría. |

### Reglas que definen el proyecto

- **Privacidad de la ubicación.** La dirección, el teléfono y el punto exacto de una solicitud solo los ven quien la creó, un administrador y quien tiene la asignación activa (hasta que el solicitante confirma). El mapa público muestra solo cantidades aproximadas por zona.
- **Nada se borra.** Usuarios, organizaciones, centros, solicitudes, asignaciones e historial usan baja lógica; la base bloquea los `DELETE`.
- **Aprobación humana.** Una organización o un centro no aparece en el mapa hasta que un administrador lo aprueba. Si un centro aprobado cambia su identidad (nombre, tipo, zona, dirección o ubicación) vuelve a revisión.
- **La atención se cierra con la confirmación de quien pidió ayuda.** El voluntario u organización completa; la persona confirma; recién ahí el recurso cuenta como resuelto y entra en las métricas.
- **Impacto medido por persona alcanzada por recurso** (una persona puede contarse en más de un recurso).

## Arquitectura

```
AyudaYa.Web      ASP.NET Core MVC: controladores, vistas Razor, JS propio (sin CDN)
AyudaYa.Negocio  Servicios de aplicación: reglas, permisos y casos de uso
AyudaYa.Data     Entidades y DbContext (EF Core + Npgsql + Identity)
AyudaYa.Dominio  (reservado)
AyudaYa.Tests    xUnit: reglas puras + integración contra PostgreSQL
database/        Esquema PostgreSQL completo (triggers, vistas, datos iniciales)
docs/prototipo/  Prototipos HTML originales de cada zona (referencia visual)
scripts/         Preparación de la base local
```

- La **web no accede a la base**: los controladores llaman a servicios de `Negocio`. La autorización vive en `Negocio` (cada método verifica quién actúa); la base protege los invariantes estructurales (estados válidos, una asignación activa por recurso, último responsable, hitos coherentes).
- El esquema es SQL puro (`database/AyudaYa_esquema_completo.sql`); **no hay migraciones de EF**.

## Cómo correrlo en tu máquina

Requisitos: **.NET 10 SDK** y **PostgreSQL 16 o 17** (por ejemplo `winget install PostgreSQL.PostgreSQL.17`).

1. Preparar la base (crea `ayudaya` y `ayudaya_test`, aplica el esquema, crea un rol con permisos mínimos y guarda los secretos con `dotnet user-secrets`; pide la contraseña de `postgres`):

   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\preparar-db-local.ps1
   ```

2. Arrancar:

   ```bash
   dotnet run --project AyudaYa.Web
   ```

   Al primer arranque se crea el administrador y, si elegiste cargar datos de ejemplo (solo en `Development`), una ciudad con zonas, organizaciones, centros, personas con distintos roles y solicitudes en todos los estados. La contraseña de los usuarios de ejemplo se imprime una vez en la consola (`usuario@`, `voluntario@`, `camila@`, `responsable@`, `miembro@`, `defensa@`, `pendiente@ejemplo.test`).

3. Probar:

   ```bash
   dotnet test
   ```

   Las pruebas de integración usan `ayudaya_test` (secreto `ConnectionStrings:AyudaYaTest` o variable `AYUDAYA_TEST_CONN`); sin esa conexión se omiten y corren solo las reglas puras.

### Base compartida del equipo

Para que todos usen la misma base (sin instalar PostgreSQL) mirá [docs/base-compartida.md](docs/base-compartida.md).

### Configuración (user-secrets o variables de entorno)

| Clave | Para qué |
|---|---|
| `ConnectionStrings:AyudaYa` (o `AYUDAYA_CONN`) | Conexión a PostgreSQL |
| `Admin:Email`, `Admin:Password`, `Admin:Nombre` | Primer administrador (solo si todavía no hay ninguno) |
| `Seed:Demo` = `true`, `Seed:PasswordDemo` | Datos de ejemplo (solo en `Development`) |
| `Correo:Host`, `Correo:Puerto`, `Correo:Usuario`, `Correo:Clave`, `Correo:Remitente` | SMTP para el correo de confirmación. Sin host, el correo queda en el log y, en desarrollo, el enlace se muestra en el perfil |

Ninguna credencial va en el repositorio.

## Seguridad

- Contraseñas con ASP.NET Core Identity (hash + bloqueo tras 5 intentos); cookie `HttpOnly`, `SameSite=Lax`; la sesión se revalida cada minuto, así que desactivar a alguien la corta.
- Antiforgery obligatorio en todos los `POST`; límite de intentos en login/registro/reenvío.
- Cabeceras: CSP (`script-src 'self'`, sin scripts en línea), `X-Frame-Options`, `nosniff`, `Referrer-Policy`, `Permissions-Policy`.
- Correo confirmado obligatorio para pedir ayuda, ser voluntario o registrar una organización.
- En producción configurá `AllowedHosts` con tu dominio (los enlaces de los correos se arman con el host de la petición) y servilo siempre por HTTPS.

## Límites conocidos

- No hay recuperación de contraseña por correo (el enlace "¿Olvidaste tu contraseña?" lo avisa).
- Las solicitudes no se pueden editar una vez creadas: se cancelan y se vuelven a cargar.
- El envío de correo usa `System.Net.Mail` (SMTP con STARTTLS); para volumen alto conviene un proveedor transaccional.
- El mapa usa los mosaicos públicos de OpenStreetMap (sujetos a su política de uso); para producción conviene un proveedor propio.


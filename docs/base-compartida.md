# Base de datos compartida del equipo (Neon)

Todo el equipo puede trabajar contra **la misma base PostgreSQL en la nube** (Neon, plan gratuito, sin tarjeta). Así nadie tiene una base distinta y los datos de ejemplo son los mismos para todos.

> La cadena de conexión contiene una contraseña. **No va en el repositorio ni en el chat del grupo público**: se pasa en privado y cada integrante la guarda en sus *user-secrets*.

## Cómo conectarte

1. Pedile a quien administra la base (Leonel) la cadena de conexión del rol `ayudaya_app`.
2. Guardala en tu máquina (una sola vez):

   ```powershell
   cd AyudaYa.Web
   dotnet user-secrets set "ConnectionStrings:AyudaYa" "<la cadena que te pasaron>"
   ```

3. Arrancá normalmente: `dotnet run --project AyudaYa.Web`. No hace falta instalar PostgreSQL.

Para volver a tu base local (si la tenés): guardá otra vez `ConnectionStrings:AyudaYa` con la cadena local.

## Reglas para no pisarnos

- Es **una sola base compartida**: lo que cargás lo ve todo el equipo. Para pruebas destructivas usá tu base local.
- La base **no permite borrar** (baja lógica): los datos de prueba se desactivan o rechazan desde Administración, no se eliminan.
- Los cambios de **esquema** se hacen editando `database/AyudaYa_esquema_completo.sql` por pull request y se aplican **una sola vez** a la base compartida (lo hace quien administra la base). Avisen en el grupo antes.
- El rol `ayudaya_app` solo lee y escribe datos; no puede crear ni borrar tablas.

## Datos de ejemplo

La base compartida ya tiene una ciudad con zonas, organizaciones, centros, personas con distintos roles y solicitudes en todos los estados. Usuarios: `usuario@`, `voluntario@`, `camila@`, `responsable@`, `miembro@`, `defensa@` y `pendiente@ejemplo.test`. La contraseña la tiene quien administra la base.

## Límites del plan gratuito

- La base se apaga sola tras unos minutos sin uso y la primera consulta tarda unos segundos en despertarla.
- El almacenamiento es limitado (alrededor de 0,5 a 1 GB), de sobra para este proyecto.
- Sin tarjeta cargada no hay forma de que Neon cobre.

## Administración de la base (solo quien la creó)

- Consola: <https://console.neon.tech> con la cuenta de Google que la creó.
- Para rotar la contraseña del rol de la app: `ALTER ROLE ayudaya_app PASSWORD '...'` y pasar la cadena nueva al equipo.

## Correo (confirmación de cuenta)

La app necesita enviar un correo para confirmar cada cuenta nueva. Render (plan gratis) **bloquea los puertos SMTP**, así que se usa la **API HTTPS de Brevo** (plan gratis: 300 correos por día, sin tarjeta). Se activa con variables de entorno (en Render, y en *user-secrets* si se quiere probar en local):

| Variable | Valor |
|---|---|
| `Correo__ApiKey` | clave de la API de Brevo (empieza con `xkeysib-`) |
| `Correo__Remitente` | remitente verificado en Brevo |
| `Correo__NombreRemitente` | `AyudaYa` |

Sin `Correo__ApiKey` la app no envía nada: el mensaje solo queda en el log del servidor y las cuentas nuevas no pueden confirmarse. Con un remitente de Gmail, Brevo reemplaza la dirección de envío y los mensajes pueden caer en spam; con un dominio propio verificado llegan mejor.

## App en la nube (Render, plan gratuito)

La rama `leo_pruebas` está desplegada en **https://ayudaya.onrender.com** y usa esta misma base de Neon.

- Se **duerme tras 15 minutos sin visitas**: la primera visita tarda cerca de 1 minuto en despertarla.
- Render construye con el `Dockerfile` del repositorio. Al ser un repositorio público sin conexión a GitHub, **no redespliega solo**: después de subir cambios hay que entrar a Render → *Manual Deploy* → *Deploy latest commit*.
- La configuración (conexión a la base, `ForwardedHeaders__Habilitar`, `DataProtection__PersistirEnBase`) está en las variables de entorno del servicio. No se carga ninguna tarjeta, así que no hay forma de que cobre.
- Para que el equipo vea los mismos datos de ejemplo, sus usuarios son los mismos que en la base local compartida.

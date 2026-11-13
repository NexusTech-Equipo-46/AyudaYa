<#
.SYNOPSIS
    Prepara la base PostgreSQL local de AyudaYa para desarrollo y pruebas.

.DESCRIPTION
    1. Crea las bases 'ayudaya' (desarrollo) y 'ayudaya_test' (pruebas) si no existen.
    2. Aplica database/AyudaYa_esquema_completo.sql en las que estén vacías.
    3. Crea el rol 'ayudaya_app' con permisos mínimos (la aplicación no necesita
       ser superusuario ni crear tablas) y le genera una contraseña aleatoria.
    4. Guarda en user-secrets (fuera del repositorio) la cadena de conexión y las
       credenciales del primer administrador.

    No escribe contraseñas en ningún archivo del proyecto. La contraseña del
    usuario 'postgres' se pide en pantalla y solo vive en memoria mientras corre.

.PARAMETER ReiniciarPruebas
    Borra y vuelve a crear la base de pruebas desde cero.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\preparar-db-local.ps1
#>
[CmdletBinding()]
param(
    [string]$Servidor = 'localhost',
    [int]$Puerto = 5432,
    [string]$UsuarioAdmin = 'postgres',
    [string]$CarpetaPostgres = 'C:\Program Files\PostgreSQL\17\bin',
    [string]$Base = 'ayudaya',
    [string]$BasePruebas = 'ayudaya_test',
    [string]$RolApp = 'ayudaya_app',
    [switch]$ReiniciarPruebas
)

$ErrorActionPreference = 'Stop'

$psql = Join-Path $CarpetaPostgres 'psql.exe'
if (-not (Test-Path $psql)) {
    throw "No encontré psql.exe en '$CarpetaPostgres'. Indicá la carpeta con -CarpetaPostgres."
}

$raiz = Split-Path $PSScriptRoot -Parent
$esquema = Join-Path $raiz 'database\AyudaYa_esquema_completo.sql'
$web = Join-Path $raiz 'AyudaYa.Web'
$pruebas = Join-Path $raiz 'AyudaYa.Tests'
if (-not (Test-Path $esquema)) { throw "No encontré el esquema en '$esquema'." }

function Leer-Secreto([string]$pregunta) {
    $segura = Read-Host $pregunta -AsSecureString
    $puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero) }
}

function Nueva-Clave([int]$largo = 24) {
    $letras = 'abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789'
    $generador = [Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object 'byte[]' $largo
    $generador.GetBytes($bytes)
    $texto = -join ($bytes | ForEach-Object { $letras[$_ % $letras.Length] })
    # Garantiza mayúscula, minúscula y número (política de contraseñas de la app).
    return 'Aa7' + $texto
}

# --- Conexión como administrador de PostgreSQL ---
$claveAdmin = Leer-Secreto "Contraseña del usuario '$UsuarioAdmin' de PostgreSQL"
$env:PGPASSWORD = $claveAdmin
$env:PGCLIENTENCODING = 'UTF8'
$env:PGCONNECT_TIMEOUT = '8'

function Invoke-Psql([string]$db, [string]$sql) {
    $anterior = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $salida = & $psql -h $Servidor -p $Puerto -U $UsuarioAdmin -d $db -v ON_ERROR_STOP=1 -q -t -A -c $sql 2>&1 }
    finally { $ErrorActionPreference = $anterior }
    if ($LASTEXITCODE -ne 0) { throw "psql falló en '$db': $($salida | Out-String)" }
    return ($salida | Out-String).Trim()
}

function Invoke-PsqlArchivo([string]$db, [string]$archivo) {
    $anterior = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $salida = & $psql -h $Servidor -p $Puerto -U $UsuarioAdmin -d $db -v ON_ERROR_STOP=1 -q -f $archivo 2>&1 }
    finally { $ErrorActionPreference = $anterior }
    if ($LASTEXITCODE -ne 0) { throw "No se pudo aplicar el esquema en '$db': $($salida | Out-String)" }
}

Write-Host "Probando la conexión..." -ForegroundColor Cyan
$null = Invoke-Psql 'postgres' 'SELECT 1'
Write-Host "  Conexión correcta." -ForegroundColor Green

function Asegurar-Base([string]$nombre, [bool]$reiniciar) {
    if ($reiniciar) {
        Write-Host "Reiniciando '$nombre'..." -ForegroundColor Yellow
        $null = Invoke-Psql 'postgres' "DROP DATABASE IF EXISTS $nombre WITH (FORCE)"
    }
    $existe = Invoke-Psql 'postgres' "SELECT 1 FROM pg_database WHERE datname = '$nombre'"
    if ($existe -ne '1') {
        Write-Host "Creando la base '$nombre'..." -ForegroundColor Cyan
        $null = Invoke-Psql 'postgres' "CREATE DATABASE $nombre ENCODING 'UTF8' TEMPLATE template0 LC_COLLATE 'C' LC_CTYPE 'C'"
    }
    # Sin comillas dobles en el SQL: Windows PowerShell 5.1 las pierde al llamar a psql.
    $tabla = Invoke-Psql $nombre "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'AspNetUsers'"
    if ($tabla -eq '0') {
        Write-Host "Aplicando el esquema en '$nombre'..." -ForegroundColor Cyan
        Invoke-PsqlArchivo $nombre $esquema
    } else {
        Write-Host "  '$nombre' ya tiene el esquema." -ForegroundColor DarkGray
    }
}

Asegurar-Base $Base $false
Asegurar-Base $BasePruebas $ReiniciarPruebas.IsPresent

# --- Rol de la aplicación con permisos mínimos ---
$claveApp = Nueva-Clave
$rolExiste = Invoke-Psql 'postgres' "SELECT 1 FROM pg_roles WHERE rolname = '$RolApp'"
if ($rolExiste -eq '1') {
    $null = Invoke-Psql 'postgres' "ALTER ROLE $RolApp WITH LOGIN PASSWORD '$claveApp' NOSUPERUSER NOCREATEDB NOCREATEROLE"
} else {
    $null = Invoke-Psql 'postgres' "CREATE ROLE $RolApp WITH LOGIN PASSWORD '$claveApp' NOSUPERUSER NOCREATEDB NOCREATEROLE"
}

foreach ($db in @($Base, $BasePruebas)) {
    $null = Invoke-Psql $db "GRANT CONNECT ON DATABASE $db TO $RolApp"
    $null = Invoke-Psql $db "GRANT USAGE ON SCHEMA public TO $RolApp"
    $null = Invoke-Psql $db "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO $RolApp"
    $null = Invoke-Psql $db "GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO $RolApp"
}
Write-Host "Rol '$RolApp' listo (sin permisos de administración)." -ForegroundColor Green

# --- Primer administrador y datos de ejemplo ---
Write-Host ""
Write-Host "Primer administrador de la aplicación (se crea al arrancar si todavía no hay ninguno)." -ForegroundColor Cyan
$emailAdmin = Read-Host "Correo del administrador [admin@ayudaya.test]"
if ([string]::IsNullOrWhiteSpace($emailAdmin)) { $emailAdmin = 'admin@ayudaya.test' }
$claveAdminApp = Leer-Secreto "Contraseña del administrador (Enter para generar una)"
$claveGenerada = $false
if ([string]::IsNullOrWhiteSpace($claveAdminApp)) { $claveAdminApp = Nueva-Clave 14; $claveGenerada = $true }

$cargarDemo = Read-Host "¿Cargar datos de ejemplo (ciudad, centros, usuarios y solicitudes)? [S/n]"
$demo = -not ($cargarDemo -match '^(n|no)$')

# --- user-secrets ---
function Guardar-Secreto([string]$proyecto, [string]$clave, [string]$valor) {
    $salida = & dotnet user-secrets set $clave $valor --project $proyecto 2>&1
    if ($LASTEXITCODE -ne 0) { throw "No pude guardar '$clave': $salida" }
}

$conexion = "Host=$Servidor;Port=$Puerto;Database=$Base;Username=$RolApp;Password=$claveApp"
$conexionPruebas = "Host=$Servidor;Port=$Puerto;Database=$BasePruebas;Username=$RolApp;Password=$claveApp"

Guardar-Secreto $web 'ConnectionStrings:AyudaYa' $conexion
Guardar-Secreto $web 'Admin:Email' $emailAdmin
Guardar-Secreto $web 'Admin:Password' $claveAdminApp
Guardar-Secreto $web 'Admin:Nombre' 'Administración AyudaYa'
Guardar-Secreto $web 'Seed:Demo' ($(if ($demo) { 'true' } else { 'false' }))
Guardar-Secreto $pruebas 'ConnectionStrings:AyudaYaTest' $conexionPruebas

Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Listo." -ForegroundColor Green
Write-Host "  Bases:        $Base (desarrollo) y $BasePruebas (pruebas)"
Write-Host "  Secretos:     guardados con 'dotnet user-secrets' (no están en el repositorio)"
Write-Host "  Administrador: $emailAdmin"
if ($claveGenerada) { Write-Host "  Contraseña generada del administrador: $claveAdminApp  (anotala, no se vuelve a mostrar)" -ForegroundColor Yellow }
if ($demo) { Write-Host "  Datos de ejemplo: se cargan al primer arranque; la contraseña de los usuarios de ejemplo aparece en la consola de la app." }
Write-Host ""
Write-Host "Para arrancar:   dotnet run --project AyudaYa.Web"
Write-Host "Para probar:     dotnet test"

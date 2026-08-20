# Manual de operacion local

## Requisitos

- .NET SDK 8.x
- Node.js compatible con Vite 8
- npm
- Puertos libres 5210 y 5173

## Estructura de trabajo

Los comandos asumen esta raiz:

```text
C:\Users\PC\Documents\Codex\2026-08-19\revias-mi-us\work
```

## Backend

```powershell
cd backend
dotnet restore
dotnet run --project ReviasMiUs.Api
```

Servicios:

- Estado: `http://localhost:5210/`
- Swagger: `http://localhost:5210/swagger`

Configuracion de puerto: `ReviasMiUs.Api/Properties/launchSettings.json`.

## Frontend

En otra terminal:

```powershell
cd frontend
npm install
npm run dev
```

Interfaz: `http://localhost:5173/`.

## Credenciales locales

| Rol | Email | Contrasena |
|---|---|---|
| Administrator | `admin@revias.local` | `Admin123!` |
| Cashier | `caja@revias.local` | `Caja123!` |
| Warehouse | `almacen@revias.local` | `Almacen123!` |

Son datos de desarrollo y no deben usarse en produccion.

## Probar con Swagger

1. Abrir Swagger.
2. Ejecutar `POST /api/auth/login`.
3. Copiar solamente `accessToken`.
4. Pulsar `Authorize`.
5. Escribir el token (Swagger usa esquema Bearer).
6. Ejecutar endpoints permitidos.

El refresh token se entrega como cookie HttpOnly y no aparece dentro del JSON.

## Ejemplo PowerShell

```powershell
$body = @{
  email = "admin@revias.local"
  password = "Admin123!"
} | ConvertTo-Json

$session = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5210/api/auth/login" `
  -ContentType "application/json" `
  -Body $body

$headers = @{ Authorization = "Bearer $($session.accessToken)" }
Invoke-RestMethod -Uri "http://localhost:5210/api/products" -Headers $headers
```

## Compilar y probar

Backend:

```powershell
cd backend
dotnet build ReviasMiUs.sln
dotnet test ReviasMiUs.sln
```

Frontend:

```powershell
cd frontend
npm run build
```

## Datos y reinicios

Todo dato funcional vive en memoria. Reiniciar backend restablece:

- clientes, leads y productos;
- ordenes y stock;
- usuarios y roles personalizados;
- mesas, turnos, comandas y comprobantes;
- ajustes;
- access/refresh tokens.

Las claves ASP.NET de desarrollo estan en `ReviasMiUs.Api/.keys/`, ignoradas por Git. No son datos del ERP.

## Diagnostico

### Frontend muestra backend no disponible

- comprobar `http://localhost:5210/`;
- revisar que API escuche 5210;
- verificar `VITE_API_URL`;
- comprobar que el origen sea `http://localhost:5173` por CORS.

### HTTP 401

- iniciar sesion de nuevo;
- revisar encabezado Bearer;
- la API pudo reiniciarse y perder tokens.

### HTTP 403

- el token es valido, pero el rol no posee el permiso;
- revisar `Usuarios y roles` con Administrator.

### HTTP 400

- leer propiedad JSON `error`;
- corresponde normalmente a una regla de dominio.

### Puerto ocupado

Identificar el proceso que escucha 5210/5173 y detener la instancia duplicada. No ejecutar varias APIs en memoria esperando datos compartidos.

### Advertencia NU1900

Si NuGet no puede consultar vulnerabilidades por falta de red, build puede continuar con warning. En CI/produccion debe habilitarse acceso y revisarse el reporte.

## Build de produccion frontend

```powershell
npm run build
```

Genera `frontend/dist/`. El directorio esta ignorado por Git. Debe servirse desde hosting estatico y configurar `VITE_API_URL` antes del build.

## Variables/configuracion pendientes

La API todavia no externaliza en configuracion:

- origen CORS;
- duracion de tokens;
- cookie Secure;
- credenciales/base de datos;
- parametros SUNAT.

Antes de desplegar deben moverse a `appsettings`/variables de entorno y secretos.

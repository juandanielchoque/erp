# Pruebas y calidad

## Framework

El proyecto usa xUnit 2.5.3, Microsoft.NET.Test.Sdk 17.8.0 y coverlet collector 6.0.0.

## Ejecutar

Desde `work/backend`:

```powershell
dotnet test ReviasMiUs.sln
```

Cuando ya existe restore/build:

```powershell
dotnet test ReviasMiUs.sln --no-build --no-restore
```

Estado validado al documentar: 21 pruebas aprobadas.

## Clases de prueba

### SalesOrderServiceTests

Valida el flujo principal de cotizaciones/pedidos, especialmente confirmacion, stock y errores por insuficiencia.

### RestaurantOperationsTests

Valida turno de caja y pago POS: venta bruta, efectivo esperado, comanda, comprobante y ocupacion de mesa.

### UserAccountServiceTests

Valida email duplicado y cambios de rol/estado.

### ManagementRulesTests

Valida reglas CRUD y trazabilidad, como reemplazo de lineas en borradores y operaciones de gestion.

### SecurityTests

Valida:

- salts distintos para la misma contrasena;
- ausencia de texto plano;
- verificacion correcta/incorrecta;
- emision y revocacion de tokens;
- rechazo de RUC empresarial invalido.

### RoleServiceTests

Valida:

- dependencias agregadas a roles operativos;
- rechazo de permisos exclusivos;
- bloqueo de eliminacion si un rol tiene usuarios.

## Cobertura actual

La cobertura se concentra en Domain/Application. No existen pruebas automatizadas para:

- endpoints HTTP completos con servidor de prueba;
- handler de autenticacion y todas las politicas;
- refresh concurrente y expiracion real;
- frontend React;
- accesibilidad/responsive;
- transacciones o concurrencia;
- integracion SUNAT;
- persistencia, porque aun no hay base de datos.

## Pruebas manuales recomendadas

### Seguridad

1. Sin token, `GET /api/products` debe devolver 401.
2. Cajero puede leer productos, pero PUT/DELETE debe devolver 403.
3. Rol personalizado POS puede vender, pero no administrar roles.
4. Usuario bloqueado debe perder acceso aun con token emitido.

### POS

1. Vender sin turno debe fallar.
2. Abrir turno y vender en efectivo debe aumentar efectivo esperado.
3. Tarjeta/Yape debe aumentar ventas, no efectivo esperado.
4. Salon debe ocupar mesa.
5. Entregar cocina debe enviar mesa a limpieza.

### Facturacion

1. Factura sin RUC debe fallar.
2. Verificar serie y correlativo por tipo.
3. Verificar base/IGV con tasa configurada.
4. Solo Administrator puede anular.

### Gestion

1. No eliminar producto/cliente con historial.
2. No editar mesa ocupada.
3. No desactivar usuario con turno abierto.
4. No eliminar ultimo administrador.

## Build frontend

Desde `work/frontend`:

```powershell
npm run build
```

Ejecuta TypeScript y build Vite. Actualmente no existe script `test` ni `lint`.

## Recomendaciones

- agregar `WebApplicationFactory` para API;
- crear matriz parametrizada permiso x endpoint;
- agregar Vitest + React Testing Library;
- usar Playwright para login, POS y roles;
- medir cobertura con reporte coverlet;
- agregar CI que ejecute build, test y frontend build.

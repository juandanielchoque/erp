# Referencia de API

## Convenciones

- Base local: `http://localhost:5210`.
- Swagger: `http://localhost:5210/swagger`.
- JSON usa camelCase.
- Salvo autenticacion, raiz y pedidos de mesa por QR, las rutas requieren `Authorization: Bearer <accessToken>`.
- Los permisos se indican en la ultima columna.

## Autenticacion

| Metodo | Ruta | Entrada | Salida | Acceso |
|---|---|---|---|---|
| GET | `/` | ninguna | estado y modulos | Publico |
| POST | `/api/auth/login` | `LoginRequest` | `AuthResponse` + cookie refresh | Publico |
| POST | `/api/auth/refresh` | cookie `revias_refresh` | `AuthResponse` + cookie rotada | Publico |
| POST | `/api/auth/logout` | bearer/cookie opcionales | 204 | Publico |
| GET | `/api/auth/me` | bearer | `AuthUserDto` | Autenticado |

Login:

```json
{
  "email": "admin@revias.local",
  "password": "Admin123!"
}
```

Respuesta abreviada:

```json
{
  "accessToken": "token-opaco",
  "expiresAtUtc": "2026-08-20T18:00:00Z",
  "user": {
    "id": "guid",
    "name": "Ana Mendoza",
    "email": "admin@revias.local",
    "role": "Administrator",
    "roleName": "Administrador",
    "permissions": ["dashboard.view", "products.manage"]
  }
}
```

## Clientes

| Metodo | Ruta | Entrada | Respuesta | Permiso |
|---|---|---|---|---|
| GET | `/api/customers` | - | `CustomerDto[]` | `customers.view` |
| GET | `/api/customers/search?term=x` | query | `CustomerDto[]` | `customers.view` |
| POST | `/api/customers` | `CreateCustomerRequest` | 201 `CustomerDto` | `customers.manage` |
| PUT | `/api/customers/{id}` | `UpdateCustomerRequest` | `CustomerDto` | `customers.manage` |
| DELETE | `/api/customers/{id}` | - | 204 | `customers.delete` |

```json
{
  "name": "Cliente ejemplo",
  "email": "cliente@ejemplo.pe"
}
```

Actualizacion agrega `isActive`.

## CRM

| Metodo | Ruta | Entrada | Respuesta | Permiso |
|---|---|---|---|---|
| GET | `/api/crm/leads` | - | `LeadDto[]` | `crm.manage` |
| GET | `/api/crm/leads/search?term=x` | query | `LeadDto[]` | `crm.manage` |
| POST | `/api/crm/leads` | `CreateLeadRequest` | 201 `LeadDto` | `crm.manage` |
| PATCH | `/api/crm/leads/{id}/stage` | `{ "stage": "Qualified" }` | `LeadDto` | `crm.manage` |
| PUT | `/api/crm/leads/{id}` | `UpdateLeadRequest` | `LeadDto` | `crm.manage` |
| DELETE | `/api/crm/leads/{id}` | - | 204 | `crm.manage` |
| GET | `/api/crm/dashboard` | - | `CrmDashboardDto` | `crm.manage` |

Etapas validas: `New`, `Qualified`, `Proposal`, `Won`, `Lost`.

## Productos

| Metodo | Ruta | Entrada | Respuesta | Permiso |
|---|---|---|---|---|
| GET | `/api/products` | - | `ProductDto[]` | `products.view` |
| GET | `/api/products/low-stock` | - | `ProductDto[]` | `products.view` |
| POST | `/api/products` | `CreateProductRequest` | 201 `ProductDto` | `products.manage` |
| PUT | `/api/products/{id}` | `UpdateProductRequest` | `ProductDto` | `products.manage` |
| DELETE | `/api/products/{id}` | - | 204 | `products.manage` |

```json
{
  "name": "Plato",
  "sku": "FON-100",
  "unitPrice": 25.0,
  "stockQuantity": 20,
  "reorderPoint": 5,
  "category": "Fondos"
}
```

Actualizacion agrega `isActive`.

## Cotizaciones y pedidos

| Metodo | Ruta | Entrada | Respuesta | Permiso |
|---|---|---|---|---|
| GET | `/api/sales-orders?status=&customerId=` | query opcional | `SalesOrderDto[]` | `sales.manage` |
| GET | `/api/sales-orders/{id}` | - | `SalesOrderDto` | `sales.manage` |
| POST | `/api/sales-orders` | `CreateSalesOrderRequest` | 201 `SalesOrderDto` | `sales.manage` |
| PUT | `/api/sales-orders/{id}` | `CreateSalesOrderRequest` | `SalesOrderDto` | `sales.manage` |
| DELETE | `/api/sales-orders/{id}` | - | 204 | `sales.manage` |
| POST | `/api/sales-orders/{id}/confirm` | - | `SalesOrderDto` | `sales.manage` |
| POST | `/api/sales-orders/{id}/cancel` | - | `SalesOrderDto` | `sales.manage` |

```json
{
  "customerId": "guid",
  "lines": [{ "productId": "guid", "quantity": 2 }],
  "validUntilUtc": "2026-08-30T00:00:00Z",
  "notes": "Entrega coordinada",
  "serviceType": "Takeaway",
  "tableNumber": null
}
```

## Dashboard

| Metodo | Ruta | Respuesta | Permiso |
|---|---|---|---|
| GET | `/api/dashboard` | `DashboardDto` | `dashboard.view` |

## Usuarios

| Metodo | Ruta | Entrada | Respuesta | Permiso |
|---|---|---|---|---|
| GET | `/api/users` | - | `UserAccountDto[]` | `users.manage` |
| POST | `/api/users` | `CreateUserAccountRequest` | 201 `UserAccountDto` | `users.manage` |
| PUT | `/api/users/{id}` | `UpdateUserAccountRequest` | `UserAccountDto` | `users.manage` |
| PATCH | `/api/users/{id}/role` | `{ "role": "Cashier" }` | `UserAccountDto` | `users.manage` |
| PATCH | `/api/users/{id}/status` | `{ "isActive": false }` | `UserAccountDto` | `users.manage` |
| DELETE | `/api/users/{id}` | - | 204 | `users.manage` |

Alta:

```json
{
  "name": "Nuevo usuario",
  "email": "usuario@revias.local",
  "role": "Cashier",
  "password": "Password123!"
}
```

## Roles y permisos

| Metodo | Ruta | Entrada | Respuesta | Permiso |
|---|---|---|---|---|
| GET | `/api/roles` | - | `RoleDto[]` | `roles.manage` |
| GET | `/api/roles/permissions` | - | `PermissionDto[]` | `roles.manage` |
| POST | `/api/roles` | `CreateRoleRequest` | 201 `RoleDto` | `roles.manage` |
| PUT | `/api/roles/{id}` | `UpdateRoleRequest` | `RoleDto` | `roles.manage` |
| DELETE | `/api/roles/{id}` | - | 204 | `roles.manage` |

```json
{
  "code": "Waiter",
  "name": "Mesero",
  "description": "Atencion de salon",
  "permissions": ["pos.use", "tables.release"]
}
```

El backend agrega dependencias y rechaza permisos exclusivos del Administrador.

## Mesas

| Metodo | Ruta | Entrada | Respuesta | Permiso |
|---|---|---|---|---|
| GET | `/api/restaurant/tables` | - | `RestaurantTableDto[]` | `tables.view` |
| POST | `/api/restaurant/tables` | `{ number, area, seats }` | 201 DTO | `tables.manage` |
| PUT | `/api/restaurant/tables/{number}` | `{ number, area, seats }` | DTO | `tables.manage` |
| DELETE | `/api/restaurant/tables/{number}` | - | 204 | `tables.manage` |
| POST | `/api/restaurant/tables/{number}/release` | - | DTO | `tables.release` |
| POST | `/api/restaurant/tables/{number}/qr/regenerate` | - | DTO con token nuevo | `tables.manage` |

El numero de mesa en ruta debe codificarse como URL. `RestaurantTableDto` incluye un `qrToken` aleatorio de 32 caracteres. Renovarlo invalida inmediatamente el enlace anterior.

## Pedidos publicos por QR

| Metodo | Ruta | Entrada | Respuesta | Acceso |
|---|---|---|---|---|
| GET | `/api/public/table-ordering/{token}/menu` | - | `PublicTableMenuDto` | Publico limitado |
| POST | `/api/public/table-ordering/{token}/orders` | `{ guestName, notes, lines }` | `PublicTableOrderDto` | Publico limitado |

El menu solo expone nombre comercial, mesa y productos activos con stock. El pedido queda confirmado, descuenta stock, ocupa la mesa si estaba libre y genera una comanda con platos y cantidades. No registra cobro ni emite comprobante; esas acciones permanecen en el POS autenticado.

Limites: 30 productos diferentes, 20 unidades por producto, nombre de 80 caracteres, observaciones de 500 caracteres y 20 solicitudes por minuto.

## Caja

| Metodo | Ruta | Entrada | Respuesta | Permiso |
|---|---|---|---|---|
| GET | `/api/cash-shifts` | - | `CashShiftDto[]` | `cash-shifts.manage` |
| POST | `/api/cash-shifts/open` | `{ userId, terminal, openingCash }` | DTO | `cash-shifts.manage` |
| POST | `/api/cash-shifts/{id}/movements` | `{ type, amount, reason }` | DTO | `cash-shifts.manage` |
| POST | `/api/cash-shifts/{id}/close` | `{ countedCash }` | DTO | `cash-shifts.manage` |

La API ignora el `userId` enviado al abrir y usa el usuario autenticado. Un operador solo puede mover/cerrar su turno; Administrator puede operar cualquier turno.

Tipos de movimiento manual: `CashIn`, `CashOut`.

## Cocina

| Metodo | Ruta | Respuesta | Permiso |
|---|---|---|---|
| GET | `/api/kitchen/tickets` | `KitchenTicketDto[]` | `kitchen.manage` |
| POST | `/api/kitchen/tickets/{id}/advance` | DTO | `kitchen.manage` |
| POST | `/api/kitchen/tickets/{id}/cancel` | DTO | `kitchen.manage` |

## Comprobantes

| Metodo | Ruta | Respuesta | Permiso |
|---|---|---|---|
| GET | `/api/billing/documents` | `FiscalDocumentDto[]` | `billing.view` |
| GET | `/api/billing/documents/{id}/receipt` | `PrintableReceiptDto` | `billing.view` |
| POST | `/api/billing/documents/{id}/cancel` | DTO | `billing.cancel` |

El ticket imprimible combina con LINQ el comprobante, la orden, sus lineas, el turno, el cajero, el medio de pago y la plantilla vigente. Esto permite reimprimir sin confiar en datos reconstruidos por el navegador.

La anulacion es interna; no envia comunicacion a SUNAT.

## POS checkout

| Metodo | Ruta | Entrada | Respuesta | Permiso |
|---|---|---|---|---|
| POST | `/api/pos/checkout` | `CompletePosSaleRequest` | `CompletePosSaleDto` | `pos.use` |

```json
{
  "userId": "guid-ignorado",
  "customerId": "guid",
  "lines": [{ "productId": "guid", "quantity": 1 }],
  "notes": "Pedido POS",
  "serviceType": "DineIn",
  "tableNumber": "Mesa 01",
  "paymentMethod": "Cash",
  "payments": [
    { "method": "Cash", "amount": 42.00 },
    { "method": "Yape", "amount": 30.00 }
  ],
  "documentType": "Receipt",
  "customerTaxId": null
}
```

Respuesta contiene `order`, `shift`, `kitchenTicket` y `fiscalDocument`.

`payments` habilita pago mixto y admite `Cash`, `Card`, `Yape`, `Plin` y `BankTransfer`. Cada parte debe ser positiva, tener máximo dos decimales y la suma debe coincidir exactamente con el total. Si se omite, `paymentMethod` mantiene compatibilidad con pagos simples.

## Ajustes

| Metodo | Ruta | Entrada/Respuesta | Permiso |
|---|---|---|---|
| GET | `/api/settings` | `SystemSettingsDto` | `settings.manage` |
| GET | `/api/settings/receipt-template` | `ReceiptTemplateDto` | `billing.view` |
| PUT | `/api/settings` | `UpdateSystemSettingsRequest` | `settings.manage` |

La plantilla incluye logo, ancho 58/80 mm, alineacion, densidad, encabezado, pie y visibilidad de empresa, RUC, cajero, cliente, pago y QR. El logo acepta PNG, JPG o WEBP hasta 375 KB.

```json
{
  "businessName": "Sabor Peruano",
  "taxId": "20123456789",
  "currency": "PEN",
  "taxRate": 18,
  "timeZone": "America/Lima",
  "receiptSeries": "B001",
  "invoiceSeries": "F001",
  "requireTableForDineIn": true
}
```

## Estados HTTP esperados

| Estado | Significado |
|---|---|
| 200 | lectura/actualizacion correcta |
| 201 | recurso creado |
| 204 | operacion sin cuerpo |
| 400 | regla de dominio o datos invalidos |
| 401 | sin sesion o token invalido |
| 403 | autenticado sin permiso |
| 404 | ruta no encontrada |

# Frontend React

## Stack

- React 19.2.4
- React DOM 19.2.4
- TypeScript 6.0.2
- Vite 8.2.0
- CSS propio sin framework visual

## Arranque

`src/main.tsx` monta `<App />` dentro de `StrictMode` y carga `styles.css`.

## App.tsx

Es el shell principal y mantiene:

- `currentUser`: identidad, rol y permisos;
- `checkingSession`: restauracion inicial;
- `activeModule`: seccion visible;
- `data`: snapshot agregado de la API;
- `error` y `loading`.

### Navegacion por permisos

Cada modulo declara uno o mas permisos. Se muestra si el usuario posee al menos uno:

| Modulo | Permiso |
|---|---|
| Vista general | `dashboard.view` |
| Punto de venta | `pos.use` |
| Mesas y caja | `tables.view` o `cash-shifts.manage` |
| Cocina | `kitchen.manage` |
| CRM | `crm.manage` |
| Ventas | `sales.manage` |
| Inventario | `products.view` |
| Clientes | `customers.manage` |
| Usuarios y roles | `users.manage` |
| Comprobantes | `billing.view` |
| Ajustes | `settings.manage` |

Las acciones internas tambien se adaptan:

- inventario editable solo con `products.manage`;
- mesas configurables solo con `tables.manage`;
- liberar mesas solo con `tables.release`;
- caja visible/operable solo con `cash-shifts.manage`;
- anulacion de comprobantes solo con `billing.cancel`.

### Carga de datos

`refresh` llama `loadErpData(currentUser.permissions)`. `loadErpData` consulta en paralelo solo endpoints compatibles con los permisos para evitar respuestas 403 innecesarias.

## Punto de venta

`PosView` controla:

- modalidad: salon, para llevar o delivery;
- mesa seleccionada;
- carrito como `Record<productId, quantity>`;
- cliente;
- efectivo inicial para apertura;
- medio de pago;
- boleta/factura y RUC;
- estados busy/mensaje.

Si no existe turno abierto propio, muestra la puerta de apertura de caja. Al cobrar invoca `createPosSale` y refresca todos los datos.

## Operacion de mesas y caja

- `TablesAndCashView`: configuracion administrativa de mesas y cierre de turnos.
- `CashierTablesView`: vista limitada por `canRelease` y `canManageCash`.
- `KitchenView`: tablero por estados y acciones avanzar/cancelar.

## ManagementViews.tsx

Contiene componentes CRUD reutilizables:

- `CrmManagement`
- `InventoryManagement`
- `CustomersManagement`
- `SalesManagement`
- `BillingManagement`

Utilidades internas:

- `Header`: encabezado y boton nuevo;
- `Modal`: dialogo editor;
- `Actions`: editar/eliminar/anular;
- `Notice`: resultado de operacion;
- `FormButtons` y `Table`.

`UsersManagement` sigue en el archivo por compatibilidad, pero App usa `SecureUsersView`; es candidato a eliminacion durante una limpieza.

## SecurityViews.tsx

### LoginView

- campos de email/contrasena;
- accesos locales de demostracion;
- controla busy/error;
- entrega el usuario autenticado a App.

### SettingsView

Carga y edita empresa, RUC, moneda, impuesto, zona horaria, series y requisito de mesa.

### SecureUsersView

Centro de seguridad con dos pestañas:

- Usuarios: alta, edicion, rol, estado, restablecimiento y eliminacion.
- Roles: tarjetas, numero de usuarios, permisos, alta, edicion y eliminacion.

El formulario agrupa permisos y deshabilita visualmente los reservados. El backend vuelve a validarlos.

## api.ts

Responsabilidades:

- interfaces TypeScript de respuestas;
- catalogo `PERMISSIONS`;
- access token en memoria;
- funciones genericas `get`, `post`, `request`, `apiFetch`;
- login, refresh, logout;
- retry unico despues de 401;
- funciones por endpoint;
- carga agregada condicional.

Variable de entorno:

```text
VITE_API_URL=http://localhost:5210
```

Si no existe, se usa esa misma URL por defecto.

## QrOrderingViews.tsx

- `GuestOrderingView`: carta publica responsive, categorias, carrito, nombre, observaciones y confirmacion del pedido.
- `TableQrModal`: genera el QR localmente, permite descargar PNG, imprimir y renovar el token.
- La ruta publica se activa con `/?table={qrToken}` antes de restaurar una sesion administrativa.
- Los controles tactiles principales miden entre 44 y 55 px.

## ReceiptViews.tsx

- `ThermalReceipt`: render compartido para vista previa, impresión inicial y reimpresión.
- `ReceiptModal`: revisión previa y llamada a `window.print()`.
- Soporta papel de 58/80 mm, logo, densidad, alineación, mensajes y campos opcionales.
- Las reglas `@media print` ocultan el ERP y conservan únicamente el rollo térmico.
- `SettingsView` contiene el diseñador en tiempo real y valida imágenes de hasta 375 KB.

## CSS

`styles.css` define el sistema visual completo:

- variables de color;
- sidebar y topbar;
- paneles, metricas, tablas y kanban;
- POS y checkout;
- mesas, turnos y cocina;
- carta tactil para comensales y plantilla imprimible de QR;
- modales y formularios;
- login, ajustes y matriz de roles;
- breakpoints en 1050, 850 y 700 px;
- animaciones `reveal`, `rise` y `bounce`.

No hay CSS Modules: las clases son globales.

## Componentes heredados/no activos

En `App.tsx`, `CrmView`, `SalesView`, `UsersView` y `CustomersView` son vistas anteriores que ya no participan en el render principal. `UsersManagement` en `ManagementViews.tsx` tampoco esta conectado. Se documentan para evitar confundirlos con la implementacion activa.

## Errores y UX

- Error de carga global: bloque de conexion con reintento.
- Errores CRUD: aviso dentro del modulo.
- Restauracion fallida: vuelve al login.
- Botones busy evitan dobles envios basicos.
- Confirmaciones destructivas usan `window.confirm`.

## Limitaciones del frontend

- Fecha de topbar esta escrita como texto fijo.
- Los módulos heredados mantienen formato PEN; el ticket térmico utiliza la moneda configurada.
- No hay router; el modulo vive en estado y no en URL.
- No hay biblioteca de formularios ni validacion de esquema.
- No hay pruebas automatizadas de componentes.
- Algunos componentes heredados deberian retirarse.

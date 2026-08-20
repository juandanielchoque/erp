# ReviasMiUs ERP

Sistema ERP modular inspirado en flujos empresariales modernos, con implementacion y arquitectura propias.

## Estructura

```text
work/
|-- backend/    API .NET 8, arquitectura hexagonal, LINQ y Swagger
`-- frontend/   React 19, TypeScript y Vite
```

## Documentacion completa

El portal tecnico se encuentra en [`docs/README.md`](docs/README.md). Incluye:

- [arquitectura hexagonal](docs/01-ARQUITECTURA.md)
- [backend y servicios](docs/02-BACKEND.md)
- [modelo de dominio](docs/03-DOMINIO.md)
- [referencia de los 63 endpoints](docs/04-API.md)
- [autenticacion, roles y 19 permisos](docs/05-SEGURIDAD.md)
- [frontend React](docs/06-FRONTEND.md)
- [flujos de negocio](docs/07-FLUJOS.md)
- [pruebas y calidad](docs/08-PRUEBAS.md)
- [manual de operacion y Swagger](docs/09-OPERACION.md)
- [limitaciones y roadmap](docs/10-ROADMAP.md)
- [mapa archivo por archivo](docs/11-CODE-MAP.md)

## Backend

```powershell
cd backend
dotnet run --project ReviasMiUs.Api
```

- API: `http://localhost:5210`
- Swagger: `http://localhost:5210/swagger`

## Frontend

```powershell
cd frontend
npm install
npm run dev
```

- Interfaz: `http://localhost:5173`

El frontend consume la API mediante HTTP. La politica CORS del backend permite el origen local del frontend.

## Tecnologias

- C# y .NET 8 para dominio, casos de uso, infraestructura y API
- LINQ para filtros, agrupaciones, proyecciones y calculos
- Swagger/OpenAPI para documentar y probar la API
- React y TypeScript para componentes y comportamiento visual
- HTML y CSS adaptable para escritorio y dispositivos moviles

## Punto de venta gastronomico

El recorrido operativo implementado es:

1. El cajero abre su turno indicando el efectivo inicial.
2. Selecciona salon, para llevar o delivery.
3. En salon elige una mesa libre.
4. Agrega productos, cliente, medio de pago y tipo de comprobante.
5. El backend confirma la venta, descuenta stock, registra el pago y ocupa la mesa.
6. La comanda aparece en cocina como pendiente, en preparacion, lista y entregada.
7. Al entregar una orden de salon, la mesa queda en limpieza y luego puede liberarse.
8. El cierre de turno calcula efectivo esperado, efectivo contado y diferencia.

Los medios de pago disponibles son efectivo, tarjeta, Yape, Plin y transferencia.

## Facturacion

El sistema genera correlativos internos `B001` para boletas y `F001` para facturas, desglosa el IGV incluido y exige un RUC de 11 digitos para factura. Los comprobantes se consultan, imprimen o reimprimen con una plantilla térmica editable de 58/80 mm desde la pantalla `Comprobantes`.

Esto todavia no constituye emision electronica legal ante SUNAT. Para produccion se debe implementar un adaptador de facturacion electronica con RUC emisor, certificado digital, credenciales SOL o proveedor autorizado, XML firmado, envio, CDR y manejo de rechazos/anulaciones.

## Usuarios

La base inicia con estos roles, pero el administrador puede crear roles operativos adicionales desde `Usuarios y roles`:

- `Administrator`: configuracion y acceso total
- `Cashier`: caja y ventas del punto de venta
- `Sales`: CRM, clientes y cotizaciones
- `Warehouse`: catalogo y movimientos de inventario

El acceso comienza siempre con inicio de sesion. Las contrasenas se almacenan mediante PBKDF2 con salt aleatorio y el backend entrega un token Bearer de 15 minutos. La renovacion usa un refresh token rotativo de 7 dias guardado en una cookie `HttpOnly`; cerrar sesion revoca ambos tokens.

Permisos principales:

- `Administrator`: usuarios, precios, eliminaciones, ajustes y acceso total
- `Cashier`: POS, su turno de caja, cocina y consultas operativas
- `Sales`: CRM, clientes y cotizaciones
- `Warehouse`: consulta de inventario y operacion de cocina

Cada rol se define mediante permisos explicitos para panel, productos, clientes, CRM, ventas, POS, mesas, caja, cocina y comprobantes. La API valida los permisos en cada solicitud; ocultar una opcion en el frontend no sustituye esta validacion.

Reglas de seguridad de roles:

- los roles nuevos reciben automaticamente las consultas requeridas por las funciones seleccionadas
- editar precios o productos, eliminar clientes, configurar mesas, anular comprobantes, administrar usuarios/roles y modificar ajustes son exclusivos de `Administrator`
- `Administrator` es un rol protegido y no puede editarse
- los cuatro roles iniciales no pueden eliminarse, aunque sus permisos operativos si pueden ajustarse
- un rol personalizado no puede eliminarse mientras tenga usuarios asignados
- el ultimo administrador activo no puede eliminarse

Credenciales locales iniciales:

- administrador: `admin@revias.local` / `Admin123!`
- cajero: `caja@revias.local` / `Caja123!`
- almacen: `almacen@revias.local` / `Almacen123!`

Estas claves son solo para desarrollo y deben cambiarse antes de publicar. En produccion tambien se requiere HTTPS y persistencia de usuarios, tokens revocados y auditoria en PostgreSQL.

## Ajustes

El administrador puede configurar nombre comercial, RUC, moneda, porcentaje de impuesto, zona horaria, series de boleta/factura y obligatoriedad de mesa. Las series y el impuesto se aplican a los nuevos comprobantes internos.

## Persistencia actual

El prototipo usa almacenamiento en memoria mediante `InMemoryErpStore`. Esto significa que los datos se reinician al detener el backend.

Para produccion se recomienda PostgreSQL con Entity Framework Core:

- licencia abierta y buen costo operativo
- transacciones seguras para ventas y stock
- soporte para reportes y consultas complejas
- adaptador reemplazable dentro de la arquitectura hexagonal

La migracion debe conservar las interfaces de repositorio y sustituir solamente los adaptadores `InMemory` por implementaciones de Entity Framework Core.

## Operacion gastronomica

El punto de venta incluye categorias de menu, atencion en salon, para llevar y delivery, 12 mesas, apertura y cierre de caja, un menu inicial de 12 platos y bebidas, comandas de cocina y comprobantes internos. El stock actual representa porciones terminadas; recetas por ingredientes, compras a proveedores, reservas y delivery integrado son las siguientes ampliaciones funcionales.

## Gestion de datos

La interfaz permite crear, editar y eliminar oportunidades CRM, clientes, productos, usuarios, mesas y cotizaciones en borrador. Las reglas de trazabilidad protegen el historial:

- clientes y productos usados en ventas se desactivan en lugar de eliminarse
- usuarios con historial de caja se desactivan en lugar de eliminarse
- solo las mesas libres pueden editarse o eliminarse
- las ventas confirmadas se cancelan para reponer stock
- los comprobantes emitidos se anulan y conservan su correlativo
- las comandas activas pueden cancelarse desde cocina

El boton `Actualizar datos` vuelve a consultar todos los modulos y refleja los cambios realizados por la API o Swagger.

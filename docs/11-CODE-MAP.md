# Code map

Mapa de navegacion del codigo fuente. Se omiten artefactos generados (`bin`, `obj`, `dist`, `node_modules`, `*.tsbuildinfo`, JS/DTS generados por TypeScript).

## Raiz

| Archivo | Funcion |
|---|---|
| `README.md` | Introduccion, ejecucion y resumen funcional |
| `.gitignore` | Excluye claves, builds y dependencias |
| `docs/` | Documentacion tecnica completa |

## Backend / solucion

| Archivo | Funcion |
|---|---|
| `backend/ReviasMiUs.sln` | Agrupa los cinco proyectos |
| `backend/ARCHITECTURE.md` | Documento arquitectonico historico; `docs/01-ARQUITECTURA.md` es la version vigente |

## ReviasMiUs.Domain

### Common

| Archivo | Contenido |
|---|---|
| `Common/Entity.cs` | Clase base con `Guid Id` |
| `Common/DomainException.cs` | Excepcion para reglas de negocio |

### CRM y clientes

| Archivo | Contenido |
|---|---|
| `Crm/Lead.cs` | `LeadStage`, entidad Lead y reglas de score/etapa |
| `Customers/Customer.cs` | Cliente, perfil y activacion |

### Inventario y pedidos

| Archivo | Contenido |
|---|---|
| `Inventory/Product.cs` | Producto, stock, reposicion y estado |
| `Orders/OrderLine.cs` | Linea, cantidad, precio y total |
| `Orders/SalesOrder.cs` | Estados, modalidad, lineas, edicion, confirmacion y cancelacion |

### Operacion

| Archivo | Contenido |
|---|---|
| `Operations/RestaurantOperations.cs` | Mesas, turnos, movimientos, comandas y comprobantes con todos sus enums |

### Usuarios y ajustes

| Archivo | Contenido |
|---|---|
| `Users/UserAccount.cs` | Enum inicial, cuenta, rol dinamico, password hash y estado |
| `Users/RoleDefinition.cs` | Codigo/nombre/permisos del rol |
| `Settings/SystemSettings.cs` | Empresa, RUC, moneda, impuesto, zona, series y mesas |

## ReviasMiUs.Application

### Abstractions

| Archivo | Puerto |
|---|---|
| `ICustomerRepository.cs` | CRUD/consulta clientes |
| `ILeadRepository.cs` | CRUD/consulta leads |
| `IProductRepository.cs` | CRUD/consulta productos |
| `ISalesOrderRepository.cs` | CRUD/consulta pedidos |
| `IUserAccountRepository.cs` | CRUD/consulta usuarios |
| `IRoleRepository.cs` | CRUD/consulta roles |
| `IOperationsRepository.cs` | mesas, turnos, comandas y comprobantes |
| `ISystemSettingsRepository.cs` | configuracion global |
| `ISecurityServices.cs` | `IPasswordHasher`, `ITokenService` y records de token |

### DTOs

| Archivo | Contratos |
|---|---|
| `Dtos/AuthDtos.cs` | login/sesion |
| `Dtos/CustomerDtos.cs` | clientes |
| `Dtos/CrmDtos.cs` | leads/dashboard CRM |
| `Dtos/DashboardDtos.cs` | panel general |
| `Dtos/ProductDtos.cs` | productos |
| `Dtos/OrderDtos.cs` | pedidos/lineas |
| `Dtos/OperationsDtos.cs` | restaurante/POS |
| `Dtos/UserDtos.cs` | usuarios |
| `Dtos/RoleDtos.cs` | roles/permisos |
| `Dtos/SettingsDtos.cs` | ajustes |

### Services

| Archivo | Caso de uso |
|---|---|
| `Services/AuthService.cs` | login, refresh, logout y me |
| `Services/CrmService.cs` | gestion de oportunidades |
| `Services/CustomerService.cs` | gestion de clientes/trazabilidad |
| `Services/ProductService.cs` | catalogo, stock bajo y trazabilidad |
| `Services/SalesOrderService.cs` | cotizaciones, stock, confirmar/cancelar |
| `Services/DashboardService.cs` | indicadores agregados |
| `Services/UserAccountService.cs` | cuentas, roles, estado y contrasenas |
| `Services/RoleService.cs` | catalogo y reglas de roles |
| `Services/RestaurantOperationsService.cs` | mesas, caja, cocina, comprobantes y checkout |
| `Services/SystemSettingsService.cs` | ajustes globales |

### Security

| Archivo | Contenido |
|---|---|
| `Security/Permissions.cs` | 19 codigos y conjunto AdministratorOnly |

## ReviasMiUs.Infrastructure

### Persistence

| Archivo | Adaptador |
|---|---|
| `InMemoryErpStore.cs` | listas compartidas y ajustes |
| `InMemoryCustomerRepository.cs` | clientes |
| `InMemoryLeadRepository.cs` | leads |
| `InMemoryProductRepository.cs` | productos |
| `InMemorySalesOrderRepository.cs` | pedidos |
| `InMemoryUserAccountRepository.cs` | usuarios |
| `InMemoryRoleRepository.cs` | roles |
| `InMemoryOperationsRepository.cs` | restaurante |
| `InMemorySystemSettingsRepository.cs` | ajustes |
| `SeedData.cs` | clientes, menu, leads, roles, usuarios y mesas iniciales |

### Security

| Archivo | Adaptador |
|---|---|
| `Security/Pbkdf2PasswordHasher.cs` | PBKDF2 SHA-256 |
| `Security/InMemoryTokenService.cs` | tokens opacos, hash, expiracion y revocacion |

## ReviasMiUs.Api

| Archivo | Funcion |
|---|---|
| `Program.cs` | DI, middleware, seed y todos los endpoints |
| `Security/TokenAuthenticationHandler.cs` | valida bearer y crea claims |
| `Properties/launchSettings.json` | URLs/perfiles locales |
| `appsettings.json` | configuracion ASP.NET base |
| `appsettings.Development.json` | configuracion Development |
| `ReviasMiUs.Api.http` | ejemplos manuales HTTP |
| `.keys/` | claves locales ignoradas por Git |

## ReviasMiUs.Tests

| Archivo | Area |
|---|---|
| `SalesOrderServiceTests.cs` | pedidos/stock |
| `RestaurantOperationsTests.cs` | POS/caja/restaurante |
| `UserAccountServiceTests.cs` | usuarios |
| `ManagementRulesTests.cs` | reglas CRUD/trazabilidad |
| `SecurityTests.cs` | hash, tokens y ajustes |
| `RoleServiceTests.cs` | permisos/roles |

## Frontend

| Archivo | Funcion |
|---|---|
| `frontend/src/main.tsx` | entry point React |
| `frontend/src/App.tsx` | shell, navegacion, dashboard, POS, mesas, cocina y vistas de lectura |
| `frontend/src/api.ts` | contratos, token, HTTP y funciones API |
| `frontend/src/ManagementViews.tsx` | CRUD CRM/productos/clientes/ventas/comprobantes |
| `frontend/src/SecurityViews.tsx` | login, ajustes, usuarios y roles |
| `frontend/src/QrOrderingViews.tsx` | carta publica tactil y administracion visual de QR por mesa |
| `frontend/src/ReceiptViews.tsx` | ticket térmico, vista previa, impresión y reimpresión |
| `frontend/src/styles.css` | sistema visual responsive completo |
| `frontend/src/vite-env.d.ts` | tipos Vite |
| `frontend/vite.config.ts` | React plugin y puerto 5173 |
| `frontend/tsconfig*.json` | configuracion TypeScript |
| `frontend/package.json` | dependencias y scripts |

## Puntos de entrada para cambios frecuentes

| Cambio | Empezar en |
|---|---|
| Nueva regla de negocio | entidad Domain + servicio Application + prueba |
| Nuevo endpoint | DTO + servicio + `Program.cs` + `api.ts` |
| Nuevo permiso | `Permissions.cs`, seed, endpoint, `PERMISSIONS` y modulo React |
| Nueva persistencia | interfaz existente + adaptador Infrastructure + DI |
| Cambio POS | `PosView`, `createPosSale`, `CompleteSale` y entidades afectadas |
| Cambio facturacion | `FiscalDocument`, `RestaurantOperationsService`, DTO y vista Billing |
| Cambio visual | componente TSX y `styles.css` |

## Codigo heredado a revisar

- `App.tsx`: `CrmView`, `SalesView`, `UsersView`, `CustomersView` no se usan en el render activo.
- `ManagementViews.tsx`: `UsersManagement` fue reemplazado por `SecureUsersView`.
- `vite.config.js`, `vite.config.d.ts` y `*.tsbuildinfo` son artefactos generados y no deben editarse manualmente.

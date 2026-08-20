# Backend .NET

## Solucion

La solucion `backend/ReviasMiUs.sln` usa .NET 8, nullable reference types e implicit usings.

| Proyecto | Tipo | Referencias | Responsabilidad |
|---|---|---|---|
| `ReviasMiUs.Domain` | Class library | ninguna | Entidades e invariantes |
| `ReviasMiUs.Application` | Class library | Domain | Casos de uso, DTOs y puertos |
| `ReviasMiUs.Infrastructure` | Class library | Application, Domain | Adaptadores en memoria y seguridad |
| `ReviasMiUs.Api` | ASP.NET Core Web | Application, Infrastructure | HTTP, DI, Swagger y middleware |
| `ReviasMiUs.Tests` | xUnit | Application, Domain, Infrastructure | Pruebas unitarias |

Dependencia externa principal de API: `Swashbuckle.AspNetCore 6.6.2`.

## Composition root

`ReviasMiUs.Api/Program.cs` registra todos los repositorios como singleton porque comparten el almacen en memoria. Los servicios de aplicacion son scoped. El token service y password hasher son singleton.

Orden relevante del pipeline:

1. CORS para `http://localhost:5173`.
2. Autenticacion Bearer personalizada.
3. Autorizacion por claims `permission`.
4. Middleware que traduce `DomainException` a JSON 400.
5. Endpoints publicos de autenticacion y grupo `/api` protegido.

## Servicios de aplicacion

### AuthService

- `Login`: valida email, estado y hash; emite tokens.
- `Refresh`: valida refresh token, comprueba usuario, rota token y emite sesion.
- `Logout`: revoca access y refresh token.
- `Me`: devuelve identidad, nombre de rol y permisos actuales.

### CrmService

- crea, lista, busca, actualiza etapa, actualiza y elimina leads;
- construye dashboard CRM agrupando por etapa con LINQ.

### CustomerService

- crea, lista, busca y actualiza clientes;
- al eliminar consulta ordenes: si existe historial, exige desactivar.

### ProductService

- crea catalogo validando SKU unico;
- lista y ordena productos;
- filtra bajo stock;
- actualiza todos los datos operativos;
- impide eliminar productos con historial, solicitando desactivacion.

### SalesOrderService

- crea cotizaciones y consolida lineas repetidas por producto;
- filtra por estado/cliente;
- edita o elimina solo borradores;
- confirma verificando vigencia y stock;
- descuenta stock al confirmar;
- cancela y repone stock si estaba confirmada.

### DashboardService

Usa LINQ para contar leads, clientes, productos, alertas, pedidos confirmados, valor abierto y SKUs bajos.

### UserAccountService

- valida emails unicos y roles existentes;
- hashea contrasenas al crear/restablecer;
- cambia perfil, rol y estado;
- protege usuarios con turnos y al ultimo administrador.

### RoleService

- expone catalogo de 19 permisos;
- crea/edita roles operativos;
- rechaza permisos reservados;
- agrega dependencias funcionales;
- protege rol Administrator, roles de sistema y roles asignados.

### RestaurantOperationsService

Coordina mesas, caja, cocina, pedidos, inventario, pagos y comprobantes. `CompleteSale` es el caso de uso principal del POS.

### SystemSettingsService

Consulta y actualiza la configuracion empresarial global.

## DTOs

Los DTOs desacoplan JSON de las entidades. Grupos:

| Archivo | Contratos |
|---|---|
| `AuthDtos.cs` | login, usuario autenticado, respuesta y resultado interno |
| `CustomerDtos.cs` | cliente, alta y actualizacion |
| `CrmDtos.cs` | lead, alta, etapa, actualizacion y dashboard CRM |
| `ProductDtos.cs` | producto, alta y actualizacion |
| `OrderDtos.cs` | lineas, solicitud y respuesta de orden |
| `OperationsDtos.cs` | mesas, caja, cocina, comprobantes y checkout |
| `UserDtos.cs` | cuenta, alta, rol, estado y actualizacion |
| `RoleDtos.cs` | rol, alta, actualizacion y catalogo de permisos |
| `SettingsDtos.cs` | lectura/actualizacion de ajustes |
| `DashboardDtos.cs` | resumen general |

## Repositorios

Las interfaces viven en Application. Los adaptadores actuales operan sobre listas de `InMemoryErpStore`.

`InMemoryOperationsRepository` concentra cuatro conjuntos relacionados: mesas, turnos, comandas y comprobantes. Su metodo privado `Replace` actualiza por `Id`.

`InMemorySystemSettingsRepository.Update` no reemplaza el objeto porque `SystemSettingsService` modifica la misma instancia mantenida por el store.

## Uso de LINQ

LINQ se usa para:

- ordenar resultados antes de mapear DTOs;
- buscar coincidencias sin distincion de mayusculas;
- filtrar stock bajo;
- agrupar lineas repetidas de un pedido;
- sumar totales y movimientos de caja;
- calcular dashboard y embudo CRM;
- contar correlativos internos;
- validar dependencias e historial;
- proyectar entidades a DTOs con `Select`.

## Manejo de errores

- Regla de negocio invalida: `DomainException` -> HTTP 400 `{ "error": "..." }`.
- Falta de autenticacion/token vencido: HTTP 401.
- Falta de permiso: HTTP 403.
- Login o refresh invalido: HTTP 401 sin detalle para no filtrar informacion.
- Ruta inexistente: HTTP 404.

## Datos iniciales

`SeedData.Initialize` crea:

- 3 clientes;
- 12 productos gastronomicos;
- 3 oportunidades CRM;
- 4 roles del sistema;
- 3 usuarios;
- 12 mesas, 8 en salon y 4 en terraza.

No se crea un usuario `Sales` inicial aunque el rol existe.

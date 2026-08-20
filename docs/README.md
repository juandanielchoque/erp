# Documentacion tecnica de ReviasMiUs ERP

Este directorio describe el sistema implementado en `work/`. La documentacion refleja el codigo actual: backend .NET 8 con arquitectura hexagonal, frontend React, seguridad por tokens y permisos, POS gastronomico, caja, cocina y comprobantes internos.

## Ruta de lectura recomendada

1. [Arquitectura](01-ARQUITECTURA.md): capas, dependencias y decisiones estructurales.
2. [Backend](02-BACKEND.md): proyectos .NET, servicios, DTOs, repositorios y LINQ.
3. [Modelo de dominio](03-DOMINIO.md): entidades, estados, relaciones y reglas.
4. [Referencia API](04-API.md): endpoints, permisos, solicitudes y respuestas.
5. [Seguridad](05-SEGURIDAD.md): login, tokens, contrasenas, roles y permisos.
6. [Frontend](06-FRONTEND.md): componentes React, estado, navegacion y cliente HTTP.
7. [Flujos de negocio](07-FLUJOS.md): POS, caja, mesas, cocina, ventas y facturacion.
8. [Pruebas](08-PRUEBAS.md): cobertura actual y comandos de verificacion.
9. [Operacion](09-OPERACION.md): instalacion, ejecucion, Swagger y diagnostico.
10. [Limitaciones y roadmap](10-ROADMAP.md): estado de prototipo y camino a produccion.
11. [Code map](11-CODE-MAP.md): mapa archivo por archivo del codigo fuente.

## Resumen del sistema

| Area | Implementacion actual |
|---|---|
| Backend | ASP.NET Core Minimal API sobre .NET 8 |
| Arquitectura | Hexagonal: Domain, Application, Infrastructure y API |
| Frontend | React 19, TypeScript 6 y Vite 8 |
| Persistencia | Memoria mediante `InMemoryErpStore` |
| Seguridad | PBKDF2, access token opaco, refresh token rotativo y cookie HttpOnly |
| Autorizacion | 19 permisos y roles configurables |
| API interactiva | Swagger/OpenAPI |
| Pruebas | xUnit, 21 pruebas automatizadas |
| Facturacion | Boletas/facturas internas; integracion SUNAT pendiente |

## Directorios principales

```text
work/
|-- backend/
|   |-- ReviasMiUs.Api/
|   |-- ReviasMiUs.Application/
|   |-- ReviasMiUs.Domain/
|   |-- ReviasMiUs.Infrastructure/
|   `-- ReviasMiUs.Tests/
|-- frontend/
|   `-- src/
|-- docs/
`-- README.md
```

## Estado documental

Los documentos explican comportamiento, responsabilidades y limites. Swagger sigue siendo la fuente ejecutable para contratos HTTP y el codigo fuente es la fuente definitiva ante cualquier diferencia.

# Limitaciones y roadmap

## Estado actual

El sistema es un prototipo funcional avanzado para validar arquitectura y operacion gastronomica. No debe tratarse aun como sistema contable o fiscal de produccion.

## Limitaciones criticas

### Persistencia

- Todos los datos se pierden al reiniciar.
- No hay migraciones, indices, restricciones SQL ni respaldos.
- No hay control de concurrencia.

### Atomicidad del POS

`CompleteSale` ejecuta varias escrituras secuenciales. Si una operacion falla a mitad, puede quedar orden confirmada sin completar el resto. La base de datos debe envolver venta, stock, pago, mesa, comanda y comprobante en una transaccion.

### Facturacion

- Documentos son internos.
- No hay SUNAT, XML UBL, certificado, CDR ni QR.
- Correlativo se calcula contando documentos; no es seguro con concurrencia.
- Anulacion no crea nota de credito ni revierte pago/stock.
- Validacion fiscal de RUC es basica.

### Seguridad

- Tokens y revocaciones viven en memoria.
- Cookie refresh usa `Secure=false` en local.
- No hay rate limiting, MFA, recuperacion ni bloqueo por intentos.
- No existe auditoria de cambios.

### Frontend

- No hay router ni URLs por modulo.
- Moneda visual fijada a PEN.
- Fecha de topbar fija.
- No hay suite de tests.
- Existen componentes heredados sin uso.

## Roadmap recomendado

### Fase 1: PostgreSQL y EF Core

1. Crear `DbContext` en Infrastructure.
2. Mapear entidades y owned collections.
3. Implementar repositorios EF bajo interfaces existentes.
4. Crear migraciones e indices unicos para email, SKU, rol y correlativos.
5. Agregar transaccion al checkout.
6. Persistir ajustes, usuarios, roles y auditoria.
7. Crear estrategia de seed solo para Development.

### Fase 2: robustez operativa

1. Recetas e ingredientes.
2. Kardex, movimientos y mermas.
3. Proveedores, compras y recepcion.
4. Reservas de mesas.
5. Direcciones y estados delivery.
6. Impresion de comandas/tickets.
7. Apertura/cierre con arqueo y aprobaciones.

### Fase 3: facturacion electronica Peru

1. Datos fiscales completos de emisor/cliente.
2. Correlativos transaccionales por serie/tipo.
3. XML UBL 2.1.
4. Firma digital.
5. Adaptador SUNAT/OSE/PSE.
6. CDR y estados aceptado/rechazado.
7. Resumen diario y comunicacion de baja.
8. Notas de credito/debito.
9. PDF/QR y envio por correo.
10. Reconciliacion y reintentos idempotentes.

### Fase 4: seguridad de produccion

1. HTTPS, cookie Secure y HSTS.
2. Persistencia/rotacion de sesiones.
3. Rate limiting y bloqueo.
4. Politica de contrasenas/recuperacion.
5. Auditoria por usuario, IP y accion.
6. Gestion de secretos y claves.
7. Pruebas OWASP y escaneo de dependencias.

### Fase 5: calidad y despliegue

1. API integration tests.
2. Tests React y E2E.
3. CI/CD.
4. Contenedores y health checks.
5. Logging estructurado, metricas y trazas.
6. Backups, restauracion y monitoreo.

## Deuda tecnica identificada

- Dividir `Program.cs` por modulos de endpoints.
- Dividir `App.tsx` y `styles.css` en componentes/areas.
- Eliminar vistas heredadas no usadas.
- Unificar idioma de mensajes.
- Extraer reloj/moneda de ajustes reales.
- Agregar validacion de solicitudes con esquema.
- Separar anulacion fiscal de cancelacion comercial.
- Modelar permisos administrativos con estrategia extensible si se necesitan gerentes con privilegios parciales.

## Criterios minimos antes de produccion

- base de datos durable y backups probados;
- checkout transaccional e idempotente;
- correlativos seguros;
- HTTPS y secretos externos;
- auditoria;
- pruebas de autorizacion y E2E;
- facturacion homologada si se usara legalmente;
- plan de soporte, monitoreo y recuperacion.

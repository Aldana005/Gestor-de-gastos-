# GASTITO – Estado del Proyecto y Resumen de Continuidad

> **Documento de Contexto Rápido**  
> Este archivo resume el estado técnico, las decisiones arquitectónicas y el avance por fases del proyecto **GASTITO** para retomar el desarrollo sin necesidad de reanalizar toda la solución.

---

## 1. Identificación y Objetivos

- **Nombre del proyecto**: GASTITO (Gestor de Ingresos y Gastos).
- **Propósito**: API REST y Web App para educación y control financiero personal (saber cuánto entró, cuánto salió y cuánto se debe en cuotas).
- **Moneda**: Exclusivamente Pesos Argentinos (**ARS**).
- **Usuario**: Usuario único fijo (`admin` / `admin`), sin registro de usuarios ni `UserId` en las tablas.
- **Autenticación**: JWT emitido por `AuthManager` (sin ASP.NET Identity ni refresh token). Todos los endpoints salvo `/auth/login` y `/health` requieren Bearer Token.
- **Manejo de Fechas (UTC)**:
  - **Fecha de movimientos**: Día calendario guardado en UTC a las `00:00:00` (`DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc)`).
  - **Auditoría** (`CreatedAtUtc`, `DeletedAtUtc`, `FechaPagoUtc`): Hora UTC real (`DateTime.UtcNow`).

---

## 2. Arquitectura de la Solución (N-Capas)

La solución está en [.NET 8 (LTS)](file:///c:/Users/Admin/Desktop/Gestor-de-gastos-/Gastito.sln) estructurada en 5 capas desacopladas con Inyección de Dependencias encadenada:

```
Gastito.Api -> Gastito.Presentation -> Gastito.BusinessLogic -> Gastito.DataAccess -> Gastito.Entities
```

1. **`Gastito.Entities`**:
   - Modelos de datos anémicos: `Category`, `Tarjeta`, `Transaction`, `HistorialMonto`, `PagoCuota`.
   - Enums: `TipoMovimiento` (Ingreso, Gasto), `Frecuencia` (Eventual, Fijo), `MedioPago` (Tarjeta, Efectivo, Transferencia), `TipoTarjeta` (Credito, Debito), `Moneda` (ARS).
2. **`Gastito.DataAccess`**:
   - `AppDbContext` con EF Core 8 (Npgsql) configurado en Docker PostgreSQL.
   - Configuraciones Fluent API en `Configurations/` con **Global Query Filter** para *Soft Delete* (`builder.HasQueryFilter(t => !t.IsDeleted);`).
   - Stores implementados: `CategoryStore`, `TarjetaStore`, `TransactionStore`.
3. **`Gastito.BusinessLogic`**:
   - Managers con validaciones y reglas de negocio: `AuthManager`, `CategoryManager`, `TarjetaManager`, `TransactionManager`.
   - Excepciones de negocio personalizadas: `BusinessException`.
4. **`Gastito.Presentation`**:
   - Controllers: `AuthController`, `CategoriasController`, `TarjetasController`, `MovimientosController`, `HealthController`.
   - DTOs / Inputs: `CategoryCreateInput`, `TransactionCreateInput`, `TransactionUpdateInput`, `TransactionFilterInput`, etc.
   - Outputs: `TransactionOutput`, `PagedResult<T>`, `CategoryOutput`, `TarjetaOutput`.
   - Validaciones: `FluentValidation` en capa Presentation para validar contratos de entrada.
   - Errores de API: Formato RFC 7807 `ProblemDetails` y `HttpValidationProblemDetails`.
5. **`Gastito.Api`**:
   - Arranque, configuración de Swagger/OpenAPI con Bearer auth, CORS habilitado para frontend local (`localhost:3000`, `localhost:5173`).
6. **`Gastito.Tests`**:
   - Proyecto xUnit con base de datos en memoria (`Microsoft.EntityFrameworkCore.InMemory`) y pruebas de validaciones con FluentValidation TestHelper.

---

## 3. Estado de Avance por Fases

| Fase | Nombre | Estado | Descripción Clave |
| :--- | :--- | :---: | :--- |
| **0** | Cimientos | ✅ Completada | Solución .sln, 5 capas, DI encadenada, docker-compose con PostgreSQL, Swagger y CORS. |
| **1** | Modelo y persistencia | ✅ Completada | Entidades, Fluent API, migración inicial PostgreSQL, seed de 4 categorías (Supermercado, Hogar, Transporte, Ingreso Principal). |
| **2** | Login | ✅ Completada | Usuario fijo en configuración, `AuthManager`, JWT Bearer, protección de endpoints. |
| **3** | Categorías y Tarjetas | ✅ Completada | CRUD completo, seed no editable/borrable, validación de no borrado si existen movimientos asociados. |
| **4** | Movimientos básicos | ✅ Completada | CRUD en `/api/v1/movimientos`, paginación (`PagedResult`), filtros por fecha/tipo/categorías, medios de pago condicionales, validación de categoría por tipo, soft delete auditado. |
| **5** | **Fijos** | ⏳ **SIGUIENTE FASE** | **Movimientos recurrentes (mensuales)**: fecha inicio y fecha fin opcional, sin cuotas, historial de montos (`HistorialMonto`), actualización con opción `aplicarDesde = MesActual \| MesSiguiente`. |
| **6** | Cuotas | ⏳ Pendiente | Gastos en cuotas (tarjeta de crédito), cálculo de valor de cuota, absorción de redondeo en última cuota. |
| **7** | Pendientes y adelantar | ⏳ Pendiente | Listado ordenado de deudas del período, botón "Adelantar", calendario corrido e historial en `PagoCuota`. |
| **8** | Dashboard y Reportes | ⏳ Pendiente | Métricas del mes actual, gastos por categoría (torta), evolución de ingresos vs gastos (líneas 3 a 12 meses), reportes con filtros. |
| **9** | Calidad | ⏳ Pendiente | Middleware global de excepciones, logging con Serilog, tests adicionales. |
| **10**| Dockerizar la API | ⏳ Pendiente | Dockerfile multi-stage y docker-compose completo (API + PostgreSQL). |
| **11**| Telegram | ⏳ Pendiente | `TelegramBotService`, webhook, interacción mediante botones. |

---

## 4. Guía Detallada para la Próxima Sesión: FASE 5 (Fijos)

### Requerimientos de la Fase 5 (Secciones 3.4 y 12 del Documento de Decisiones):
1. **Entidad `Transaction`**:
   - Ya tiene las columnas `FechaInicio` y `FechaFin`.
   - `Frecuencia = Frecuencia.Fijo`.
   - Solo mensual. No pueden tener cuotas.
2. **Historial de Montos (`HistorialMonto`)**:
   - Columnas: `Id`, `TransactionId`, `Monto`, `VigenteDesde` (primer día del mes como UTC), `CreatedAtUtc`.
   - Cuando un movimiento fijo cambia de monto:
     - Opción 1: `MesActual` (el nuevo monto rige desde el mes en curso).
     - Opción 2: `MesSiguiente` (el nuevo monto rige desde el 1° del mes siguiente).
     - Los meses anteriores conservan su valor histórico en reportes gracias a los registros de `HistorialMonto`.
3. **Endpoints involucrados**:
   - `PUT /api/v1/movimientos/{id}?aplicarDesde=MesActual` o `MesSiguiente`:
     - Para movimientos fijos, registrar el nuevo monto en `HistorialMonto`.
4. **Validaciones**:
   - Fijo no admite cuotas.
   - `FechaFin` no puede ser anterior a `FechaInicio`.
   - `aplicarDesde` obligatorio o con valor por defecto cuando se actualiza el monto de un fijo.

---

## 5. Comandos de Trabajo Rápido

```powershell
# Ejecutar la suite completa de tests
dotnet test

# Compilar toda la solución
dotnet build

# Iniciar contenedor de base de datos PostgreSQL si se requiere
docker compose up -d

# Convención de ramas para la Fase 5:
git checkout -b fase-5-fijos
# ... al terminar y verificar tests:
git commit -m "Fase 5: Fijos - ..."
git checkout main
git merge fase-5-fijos
```

---
*Última actualización: Cierre de Fase 4 (43/43 tests pasando).*

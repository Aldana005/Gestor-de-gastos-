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
| **5** | Fijos | ✅ Completada | Movimientos recurrentes (mensuales), `FechaInicio` y `FechaFin`, sin cuotas, historial de montos (`HistorialMonto`), actualización con `aplicarDesde = MesActual | MesSiguiente`, ocurrencias mensuales en consultas. |
| **6** | Cuotas | ✅ Completada | Gastos en cuotas (tarjeta de crédito), cálculo de valor de cuota con absorción de redondeo en la última cuota, inicialización de calendario de cuotas, `CuotaCalculator`, validaciones estrictas y protección de compras con pagos. |
| **7** | Pendientes y adelantar | ✅ Completada | Endpoint `/api/v1/pendientes` con orden especificado (eventuales primero por monto desc, luego compras en cuotas que terminan en período, por cuotas pagadas desc y valor de cuota desc); botón "Adelantar" (`POST /movimientos/{id}/cuotas/adelantar`), corrimiento de calendario de cuotas e historial en `PagoCuota`. |
| **8** | **Dashboard y Reportes** | ⏳ **SIGUIENTE FASE** | **Dashboard (`/api/v1/dashboard?meses=3`)** con totales del mes actual, monto pendiente, torta por categoría, gráfico de líneas de ingresos vs gastos (3 a 12 meses) y próximas deudas; **Reportes (`/api/v1/reportes/resumen`)** con filtros por rango de fechas y categorías. |
| **9** | Calidad | ⏳ Pendiente | Middleware global de excepciones, logging con Serilog, tests adicionales. |
| **10**| Dockerizar la API | ⏳ Pendiente | Dockerfile multi-stage y docker-compose completo (API + PostgreSQL). |
| **11**| Telegram | ⏳ Pendiente | `TelegramBotService`, webhook, interacción mediante botones. |

---

## 4. Guía Detallada para la Próxima Sesión: FASE 8 (Dashboard y Reportes)

### Requerimientos de la Fase 8 (Secciones 3.9, 3.10, 9 y 10 del Documento de Decisiones):
1. **Dashboard (`GET /api/v1/dashboard?meses=3`)**:
   - Todo referido al mes actual (salvo gráfico de líneas):
     - `TotalIngresos`: ingresos del mes actual.
     - `TotalGastos`: gastos del mes actual (las cuotas cuentan solo la cuota del mes, fijos cuentan su valor en el mes).
     - `MontoPendiente`: valor de las cuotas que vencen en el mes actual y aún no están pagadas.
     - `GastosPorCategoria`: distribución para gráfico de torta (solo categorías de gasto con monto > 0 en el mes).
     - `IngresosVsGastos`: serie histórica mes a mes (por defecto 3 meses contando el actual, filtro de 3 a 12 meses).
     - `ProximasDeudas`: compras en cuotas con cuotas sin pagar, ordenadas según Pendientes:
       `{ id, descripcion, categoria, cuotasPagadas, cantidadCuotas, valorCuota, mesProximaCuota }`.
2. **Reportes (`GET /api/v1/reportes/resumen?desde&hasta&categoriaIds`)**:
   - Filtros por rango de fechas y categorías (selección múltiple):
     - `TotalIngresos` y `TotalGastos` del período.
     - `GastosPorCategoria`: distribución por categoría para el período.
     - `ProximasDeudas`: próximas deudas del período consultado.
     - (Los movimientos detallados se consultan a través de `GET /api/v1/movimientos` con los mismos filtros).

---

## 5. Comandos de Trabajo Rápido

```powershell
# Ejecutar la suite completa de tests
dotnet test

# Compilar toda la solución
dotnet build

# Iniciar contenedor de base de datos PostgreSQL si se requiere
docker compose up -d

# Convención de ramas para la Fase 8:
git checkout -b fase-8-dashboard-reportes
# ... al terminar y verificar tests:
git commit -m "Fase 8: Dashboard y Reportes - ..."
git checkout main
git merge fase-8-dashboard-reportes
```

---
*Última actualización: Cierre de Fase 7 (64/64 tests pasando).*


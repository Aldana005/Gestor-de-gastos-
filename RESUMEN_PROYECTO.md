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
| **8** | Dashboard y Reportes | ✅ Completada | **Dashboard (`/api/v1/dashboard?meses=3`)** con totales del mes actual, monto pendiente, torta por categoría, gráfico de líneas de ingresos vs gastos (3 a 12 meses) y próximas deudas; **Reportes (`/api/v1/reportes/resumen`)** con filtros por rango de fechas y categorías. |
| **9** | **Calidad** | ⏳ **SIGUIENTE FASE** | **Middleware global de excepciones (RFC 7807 ProblemDetails)**, logging estructurado con Serilog, tests unitarios e integración adicionales con `WebApplicationFactory`. |
| **10**| Dockerizar la API | ⏳ Pendiente | Dockerfile multi-stage y docker-compose completo (API + PostgreSQL). |
| **11**| Telegram | ⏳ Pendiente | `TelegramBotService`, webhook, interacción mediante botones. |

---

## 4. Guía Detallada para la Próxima Sesión: FASE 9 (Calidad)

### Requerimientos de la Fase 9 (Secciones 8, 9, 10 y 12 del Documento de Decisiones):
1. **Middleware Global de Excepciones**:
   - Captura de `BusinessException` retornando HTTP 400 Bad Request en formato `ProblemDetails` RFC 7807 (`type`, `title`, `status`, `detail`, `instance`).
   - Captura de excepciones no controladas retornando HTTP 500 Internal Server Error en formato `ProblemDetails` sin fugar stack traces en producción.
   - Preservar consistencia de respuestas con `HttpValidationProblemDetails` generado por validaciones de FluentValidation.
2. **Logging Estructurado con Serilog**:
   - Configuración en `Program.cs` / `appsettings.json` con enriquecedores (`FromLogContext`, `WithMachineName`, `WithProcessId`).
   - Salida estructurada a consola (y/o archivo según configuración).
   - Sustituir logging por defecto con Serilog en pipeline ASP.NET Core (`UseSerilogRequestLogging`).
3. **Tests de Integración con `WebApplicationFactory`**:
   - Pruebas E2E de endpoints protegidos con autenticación JWT contra la API en memoria.
   - Validación de respuestas `ProblemDetails` ante errores de negocio y errores 404/401/400.

---

## 5. Comandos de Trabajo Rápido

```powershell
# Ejecutar la suite completa de tests
dotnet test

# Compilar toda la solución
dotnet build

# Iniciar contenedor de base de datos PostgreSQL si se requiere
docker compose up -d

# Convención de ramas para la Fase 9:
git checkout -b fase-9-calidad
# ... al terminar y verificar tests:
git commit -m "Fase 9: Calidad - ..."
git checkout main
git merge fase-9-calidad
```

---
*Última actualización: Cierre de Fase 8 (75/75 tests pasando).*


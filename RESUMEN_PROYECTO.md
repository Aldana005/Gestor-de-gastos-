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
| **7** | **Pendientes y adelantar** | ⏳ **SIGUIENTE FASE** | **Listado ordenado de deudas del período y adelantos**: endpoint `/api/v1/pendientes`, botón "Adelantar", corrimiento de calendario de cuotas restantes e historial en `PagoCuota`. |
| **8** | Dashboard y Reportes | ⏳ Pendiente | Métricas del mes actual, gastos por categoría (torta), evolución de ingresos vs gastos (líneas 3 a 12 meses), reportes con filtros. |
| **9** | Calidad | ⏳ Pendiente | Middleware global de excepciones, logging con Serilog, tests adicionales. |
| **10**| Dockerizar la API | ⏳ Pendiente | Dockerfile multi-stage y docker-compose completo (API + PostgreSQL). |
| **11**| Telegram | ⏳ Pendiente | `TelegramBotService`, webhook, interacción mediante botones. |

---

## 4. Guía Detallada para la Próxima Sesión: FASE 7 (Pendientes y adelantar)

### Requerimientos de la Fase 7 (Secciones 3.6, 3.7, 9 y 10 del Documento de Decisiones):
1. **Adelantar Cuotas (`POST /api/v1/movimientos/{id}/cuotas/adelantar`)**:
   - Pide `{ cantidad: int }` (mayor a 0 y no mayor a las cuotas restantes: `cantidad <= (CantidadCuotas - CuotasPagadas)`).
   - Registra en `PagoCuota`: `TransactionId`, `CantidadCuotas`, `Monto`, `CuotaDesde`, `CuotaHasta`, `FechaPagoUtc`.
   - Incrementa `CuotasPagadas += cantidad`.
   - Corrimiento de calendario:
     - La próxima cuota pendiente pasa al mes siguiente al del pago y las demás la siguen mes a mes.
     - `FechaPrimeraCuotaPendiente` se actualiza con la fecha corrida.
     - Caso borde: si lo adelantado no supera las cuotas que ya correspondían hasta el mes del pago, el calendario no cambia.
2. **Historial de Pagos (`GET /api/v1/movimientos/{id}/cuotas/pagos`)**:
   - Devuelve la lista de adelantos registrados en `PagoCuota` para ese movimiento.
3. **Listado de Pendientes (`GET /api/v1/pendientes?desde&hasta&categoriaIds&montoMin&montoMax`)**:
   - Gastos del período (rango de fechas por defecto: mes actual).
   - Los movimientos fijos NO entran.
   - Orden exacto según especificación:
     1. Gastos eventuales sin cuotas, de mayor a menor monto.
     2. Compras en cuotas:
        a. Primero las que terminan dentro del rango (su última cuota cae en el período).
        b. Después el resto, por cuotas pagadas de mayor a menor (ej.: 3/6 antes que 1/6).
        c. En caso de empate, por valor de la cuota de mayor a menor.

---

## 5. Comandos de Trabajo Rápido

```powershell
# Ejecutar la suite completa de tests
dotnet test

# Compilar toda la solución
dotnet build

# Iniciar contenedor de base de datos PostgreSQL si se requiere
docker compose up -d

# Convención de ramas para la Fase 7:
git checkout -b fase-7-pendientes-adelantar
# ... al terminar y verificar tests:
git commit -m "Fase 7: Pendientes y adelantar - ..."
git checkout main
git merge fase-7-pendientes-adelantar
```

---
*Última actualización: Cierre de Fase 6 (58/58 tests pasando).*

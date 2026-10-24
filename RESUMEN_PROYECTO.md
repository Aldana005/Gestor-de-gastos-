# GASTITO – Estado del Proyecto y Resumen de Continuidad

> **Documento de Contexto Rápido**  
> Este archivo resume el estado técnico, las decisiones arquitectónicas y el avance por fases del proyecto **GASTITO** para retomar el desarrollo sin necesidad de reanalizar toda la solución.

---

## 1. Identificación y Objetivos

- **Nombre del proyecto**: GASTITO (Gestor de Ingresos y Gastos).
- **Propósito**: API REST y Web App para educación y control financiero personal (saber cuánto entró, cuánto salió y cuánto se debe en cuotas).
- **Moneda**: Exclusivamente Pesos Argentinos (**ARS**).
- **Usuario**: Usuario único fijo (`admin` / `admin`), sin registro de usuarios ni `UserId` en las tablas.
- **Autenticación**: JWT emitido por `AuthManager` (sin ASP.NET Identity ni refresh token). Todos los endpoints salvo `/auth/login`, `/health` y `/telegram/webhook` (protegido por token secreto) requieren Bearer Token.
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
   - Modelos de datos anémicos: `Category`, `Tarjeta`, `Transaction`, `HistorialMonto`, `PagoCuota`, `TelegramConversacion`.
   - Enums: `TipoMovimiento` (Ingreso, Gasto), `Frecuencia` (Eventual, Fijo), `MedioPago` (Tarjeta, Efectivo, Transferencia), `TipoTarjeta` (Credito, Debito), `Moneda` (ARS), `TelegramEstadoConversacion`.
2. **`Gastito.DataAccess`**:
   - `AppDbContext` con EF Core 8 (Npgsql) configurado en Docker PostgreSQL.
   - Configuraciones Fluent API en `Configurations/` con **Global Query Filter** para *Soft Delete* (`builder.HasQueryFilter(t => !t.IsDeleted);`).
   - Stores implementados: `CategoryStore`, `TarjetaStore`, `TransactionStore`, `TelegramConversacionStore`.
3. **`Gastito.BusinessLogic`**:
   - Managers con validaciones y reglas de negocio: `AuthManager`, `CategoryManager`, `TarjetaManager`, `TransactionManager`, `DashboardManager`, `ReportesManager`.
   - Servicios: `TelegramBotService` (parseo de texto directo, flujo conversacional guiado con botones, seguridad por `AuthorizedChatId`).
   - Excepciones de negocio personalizadas: `BusinessException`.
4. **`Gastito.Presentation`**:
   - Controllers: `AuthController`, `CategoriasController`, `TarjetasController`, `MovimientosController`, `DashboardController`, `ReportesController`, `PendientesController`, `TelegramController`, `HealthController`.
   - DTOs / Inputs: `CategoryCreateInput`, `TransactionCreateInput`, `TransactionUpdateInput`, `TransactionFilterInput`, `TelegramUpdate`, etc.
   - Outputs: `TransactionOutput`, `PagedResult<T>`, `CategoryOutput`, `TarjetaOutput`, `DashboardOutput`, `ReportesOutput`, `TelegramBotResponse`.
   - Middlewares: `GlobalExceptionHandlerMiddleware` (formato RFC 7807 `ProblemDetails` para `BusinessException`, `KeyNotFoundException` y 500 no controladas con `traceId`).
   - Validaciones: `FluentValidation` en capa Presentation para validar contratos de entrada.
5. **`Gastito.Api`**:
   - Arranque, configuración de Swagger/OpenAPI con Bearer auth, CORS habilitado para frontend local (`localhost:3000`, `localhost:5173`).
   - Logging estructurado con **Serilog** (`UseSerilogRequestLogging`, enrichers de proceso, máquina y contexto, salidas a consola y archivo).
   - Auto-migración en arranque (`Migrate()`) para entornos relacionales (Docker/PostgreSQL).
6. **`Gastito.Tests`**:
   - Proyecto xUnit con 96 pruebas automatizadas:
     - Tests unitarios de managers, validadores, cálculo de cuotas y middleware.
     - Tests de integración E2E con `WebApplicationFactory<Program>` (login, endpoints protegidos, ProblemDetails y webhook de Telegram).

---

## 3. Estado de Avance por Fases

| Fase | Nombre | Estado | Descripción Clave |
| :--- | :--- | :---: | :--- |
| **0** | Cimientos | ✅ Completada | Solución .sln, 5 capas, DI encadenada, docker-compose con PostgreSQL, Swagger y CORS. |
| **1** | Modelo y persistencia | ✅ Completada | Entidades, Fluent API, migración inicial PostgreSQL, seed de 4 categorías (Supermercado, Hogar, Transporte, Ingreso Principal). |
| **2** | Login | ✅ Completada | Usuario fijo en configuración, `AuthManager`, JWT Bearer, protección de endpoints. |
| **3** | Categorías y Tarjetas | ✅ Completada | CRUD completo, seed no editable/borrable, validación de no borrado si existen movimientos asociados. |
| **4** | Movimientos básicos | ✅ Completada | CRUD en `/api/v1/movimientos`, paginación (`PagedResult`), filtros por fecha/tipo/categorías, medios de pago condicionales, validación de categoría por tipo, soft delete auditado. |
| **5** | Fijos | ✅ Completada | Movimientos recurrentes (mensuales), `FechaInicio` y `FechaFin`, sin cuotas, historial de montos (`HistorialMonto`), actualización con `aplicarDesde = MesActual \| MesSiguiente`, ocurrencias mensuales en consultas. |
| **6** | Cuotas | ✅ Completada | Gastos en cuotas (tarjeta de crédito), cálculo de valor de cuota con absorción de redondeo en la última cuota, inicialización de calendario de cuotas, `CuotaCalculator`, validaciones estrictas y protección de compras con pagos. |
| **7** | Pendientes y adelantar | ✅ Completada | Endpoint `/api/v1/pendientes` con orden especificado (eventuales primero por monto desc, luego compras en cuotas que terminan en período, por cuotas pagadas desc y valor de cuota desc); botón "Adelantar" (`POST /movimientos/{id}/cuotas/adelantar`), corrimiento de calendario de cuotas e historial en `PagoCuota`. |
| **8** | Dashboard y Reportes | ✅ Completada | **Dashboard (`/api/v1/dashboard?meses=3`)** con totales del mes actual, monto pendiente, torta por categoría, gráfico de líneas de ingresos vs gastos (3 a 12 meses) y próximas deudas; **Reportes (`/api/v1/reportes/resumen`)** con filtros por rango de fechas y categorías. |
| **9** | Calidad | ✅ Completada | **Middleware global de excepciones (RFC 7807 ProblemDetails)**, logging estructurado con **Serilog**, tests de integración E2E con `WebApplicationFactory`. |
| **10**| Dockerizar la API | ✅ Completada | **Dockerfile multi-stage**, docker-compose completo (API + PostgreSQL con healthcheck), auto-migración relacional en arranque y `.dockerignore`. |
| **11**| Telegram | ✅ Completada | **`TelegramBotService`** con parseo de formato directo (`ingreso/gasto monto categoría`, `gasto monto comentario cuotas`), flujo conversacional con botones interactivos, webhook seguro con header `X-Telegram-Bot-Api-Secret-Token` y control de `AuthorizedChatId`. |

---

## 4. Guía Detallada para la Próxima Sesión: FRONTEND WEB (Antigravity)

### Requerimientos del Frontend (Secciones 4, 5 y 13 del Documento de Decisiones):
1. **Pila Tecnológica**:
   - HTML, CSS Vanilla con Tokens de diseño centralizados (`tokens.css` para colores, tipografía, espaciado y tema visual intercambiable sin tocar componentes).
   - SPA moderna (React / TypeScript / Vite).
   - Librería de gráficos ligeros (ej. Chart.js / Recharts) para tortas y gráfico de líneas (3 a 12 meses).
2. **Mapa de Pantallas**:
   - **Login**: Logo, usuario, contraseña (`admin` / `admin`), feedback de error.
   - **Dashboard Principal**:
     - Tarjetas del mes actual: Total Ingresos ("Este mes"), Total Gastos ("Este mes"), Monto Pendiente en cuotas.
     - Gráfico de torta de gastos por categoría (solo con monto en el mes).
     - Gráfico de líneas (ingresos vs gastos) con selector de meses (3 a 12).
     - Listado de "Próximas deudas" con botón "Ver más".
     - Botón flotante "+" para nuevo movimiento.
     - Enlace a pantalla de Reportes y menú.
   - **Popup de Carga ("+")**:
     - Pestañas Ingreso / Gasto.
     - Ingreso: monto, categoría (+ botón crear nueva), frecuencia (Fijo/Eventual), fecha, fecha fin (si fijo), descripción, notas.
     - Gasto: medio de pago (Efectivo/Transferencia/Tarjeta), selección de tarjeta (+ crear nueva), opción cuotas (si tarjeta de crédito), categoría (+ crear nueva), fecha, descripción, notas.
   - **Reportes y Movimientos**:
     - Filtros interactivos por rango de fechas y categorías (selección múltiple).
     - Tarjetas de totales ("del período").
     - Torta de gastos del período.
     - Tabla/lista de últimos movimientos paginados con opciones de editar y borrar (soft delete).
     - Próximas deudas del período.
   - **Pendientes y Adelantar Cuotas**:
     - Listado ordenado con desglose de progreso (ej. 3/6), valor de cuota.
     - Botón "Adelantar" cuotas con selector numérico, validación de cuotas restantes e historial de pagos.
3. **Reglas del Cliente**:
   - Moneda formateada en Pesos Argentinos: `$ 1.234,56`.
   - Fechas en formato `dd/mm/aaaa`.
   - Manejo de token Bearer en `Authorization: Bearer <token>`, redirección al login ante 401.
   - Manejo de respuestas `ProblemDetails` para errores de validación y de negocio.

---

## 5. Comandos de Trabajo Rápido

```powershell
# Ejecutar la suite completa de tests (96/96 pasando)
dotnet test

# Compilar toda la solución
dotnet build

# Levantar toda la infraestructura en contenedores (API + PostgreSQL)
docker compose up -d --build

# Iniciar la API localmente en desarrollo
dotnet run --project Gastito.Api
```

---
*Última actualización: Cierre de Fase 11 (96/96 tests pasando).*

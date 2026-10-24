# GASTITO — Frontend Web

Aplicación web mobile-first y responsive para registrar y organizar ingresos, gastos personales y compras en cuotas (moneda única: **Pesos Argentinos, ARS**).

Diseñada y construida siguiendo el sistema de diseño oficial de **Небо (Holst)**, la estética de referencia **Nexora** (glassmorphism suave, degradé azul oscuro, chips en píldora y navegación inferior fija) y el contrato de API REST v1 (.NET 8).

---

## 1. Características Implementadas

- **Mobile-First & Responsive**:
  - Diseñada para navegadores móviles (Android e iOS) con soporte para áreas seguras (`safe-area-inset`).
  - Barra de navegación inferior fija de 3 ítems (*Inicio*, *Pendientes*, *Reportes*).
  - En escritorio se adapta automáticamente con barra de navegación lateral.
  - Diseñada y probada para resoluciones desde 360 px de ancho sin desbordes.
  - Botones táctiles con altura mínima de 44 px.

- **Sistema de Diseño y Tokens Centralizados**:
  - Archivo único de tokens: [`src/styles/tokens.css`](src/styles/tokens.css) con variables CSS que alimentan Tailwind CSS y utilidades directas.
  - Paleta oficial Holst (7 tonalidades desde `#04202E` hasta `#E1F2FA`).
  - Colores semánticos acompañados de signo e iconografía (+ / −):
    - Ingreso / éxito: Verde menta (`#3DDC97`)
    - Gasto / peligro / error: Coral (`#FF6B6B`)
    - Aviso / advertencia: Ámbar (`#FFC857`)
  - Tarjetas con efecto vidrio (*glassmorphism*), borde sutil de 20% y desenfoque `backdrop-blur`.
  - Gráfico de dona de categorías con contraste mínimo AA.
  - Logo provisional: wordmark textual **GASTITO**.

- **Pantallas y Flujos**:
  - **Login**:
    - Acceso con credenciales de desarrollo: `admin` / `admin`.
    - Almacena el token y envía el encabezado `Authorization: Bearer <token>` en cada petición.
    - Si la API responde `401 Unauthorized`, limpia la sesión y redirige automáticamente al login.
    - Manejo de respuestas `ProblemDetails` y errores de red.
  - **Inicio (Dashboard — Todo del mes actual)**:
    - 4 Tarjetas: *Total ingresos — Este mes*, *Total gastos — Este mes*, *Monto pendiente* (con navegación directa a Pendientes) y *Reportes* (con navegación directa a Reportes).
    - **Estricto: No incluye sobrante ni remanente.**
    - Gráfico de torta (Donut) de gastos por categoría (solo categorías con gastos en el mes actual).
    - Gráfico de líneas de *Ingresos vs Gastos* con selector de filtro de **3 a 12 meses** (por defecto 3 meses, afectando únicamente a este gráfico).
    - Listado de *Próximas deudas* con progreso (ej. `3/6`) y botón "Ver más" que conduce a Pendientes.
    - Botón flotante `+` para abrir el popup de carga.
  - **Popup de Carga ("+")**:
    - Selector de tipo: **Ingresos** / **Gastos**.
    - *Ingreso*: Monto, Categoría (tipo ingreso + opción de crear nueva en modal secundario), Frecuencia (Fijo / Eventual), Fecha (hoy por defecto), Fecha de fin (opcional, visible solo si es Fijo), Descripción y Notas.
    - *Gasto*:
      1. Medio de pago: Tarjeta / Efectivo / Transferencia.
      2. Si es Tarjeta: Selección de tarjeta (+ opción de crear nueva con tipo crédito/débito).
      3. Si la tarjeta es de crédito: Pregunta "¿Tiene cuotas?" (Sí / No). Si es Sí: Monto total, Cantidad de cuotas (mínimo 2), y Fecha de primera cuota con calendario. Se oculta el campo de monto unitario.
      4. Frecuencia: Fijo / Eventual (las cuotas bloquean la frecuencia fija).
      5. Monto, Categoría (+ crear nueva), Fecha, Descripción y Notas.
    - Validaciones completas en cliente y visualización de errores `ProblemDetails` junto a cada campo.
  - **Reglas de Comportamiento Especiales**:
    - **Editar un movimiento Fijo con cambio de monto**: Selector para aplicar el cambio "Desde este mes" o "Desde el mes siguiente" (`aplicarDesde = MesActual | MesSiguiente`), preservando los montos históricos.
    - **Compra en cuotas con cuotas ya pagadas o adelantadas**: Bloqueo estricto de edición para monto total y cantidad de cuotas (solo se permite editar descripción, categoría y notas).
  - **Reportes (y Últimos Movimientos)**:
    - Filtros dinámicos reactivos: Selección múltiple de categorías (chips pill) y rango de fechas (desde / hasta con atajos rápidos).
    - Tarjetas de resumen: *Total ingresos del período* y *Total gastos del período*.
    - Gráfico de dona de gastos del período.
    - Listado paginado de últimos movimientos con acciones de **Editar** y **Borrar** (con diálogo de confirmación accesible).
    - Próximas deudas proyectadas del período.
  - **Pendientes**:
    - Lista que respeta estrictamente el orden devuelto por la API (gastos eventuales sin cuotas primero, luego compras en cuotas).
    - Cada compra en cuotas muestra: descripción, categoría, progreso (`3/6`), valor de cuota, mes de la próxima cuota y botón **"Adelantar"**.
    - Diálogo de **"Adelantar"**: selector numérico (mínimo 1, máximo cuotas restantes). **No utiliza en ningún momento la palabra "Pagar"**.
    - Historial desplegable de cuotas adelantadas (cantidad, monto, fecha, rango "cuotas X a Y").
    - Filtros por categoría, rango de fechas y rango de montos (mínimo y máximo).

- **Estados de Interfaz**:
  - Skeleton screens con animación *shimmer* para estados de carga.
  - Estados vacíos (*empty states*) con ilustraciones e iconografía amigable.
  - Estados de error con botón de reintentar.

---

## 2. Tecnologías Utilizadas

- **Framework**: React 19 + TypeScript + Vite.
- **Estilos**: Tailwind CSS + CSS Variables (`tokens.css`) + Glassmorphism personalizado.
- **Rutas**: React Router 7.
- **Gestión de Estado & Caché de Datos**: TanStack Query (React Query).
- **Gráficos**: Recharts (Donut y Líneas responsive).
- **Iconos**: Lucide React.
- **Validaciones**: Zod + validación desacoplada.

---

## 3. Instrucciones de Ejecución

### Prerrequisitos
- Node.js v18+ (recomendado v20+ o v24).
- npm v9+.

### Paso 1: Instalar dependencias
Desde la carpeta `frontend/`:
```bash
cd frontend
npm install --legacy-peer-deps
```

### Paso 2: Ejecutar con datos simulados (Mock por defecto)
El proyecto viene configurado para correr de inmediato con la capa mock en memoria y `localStorage`:
```bash
npm run dev
```
La aplicación abrirá en: [http://localhost:5173](http://localhost:5173).

- Credenciales de acceso:
  - **Usuario**: `admin`
  - **Contraseña**: `admin`

*(Nota: En la cabecera superior existe un botón para reiniciar los datos mock a su estado original en cualquier momento).*

### Paso 3: Conectar a la API real de .NET 8
Para conectar el frontend a tu backend ASP.NET Core:

1. Asegurate de tener corriendo la API de .NET (por ejemplo en `http://localhost:5000` o `http://localhost:5163`).
2. Editá el archivo `.env` en `frontend/.env`:
```env
VITE_API_BASE_URL=http://localhost:5000/api/v1
VITE_USE_MOCKS=false
```
3. Reiniciá el servidor de desarrollo (`npm run dev`). Todas las peticiones se enviarán automáticamente a los endpoints reales de `/api/v1` con el token JWT Bearer.

---

## 4. Estructura del Código

```text
frontend/
├── src/
│   ├── api/
│   │   ├── types.ts          # Contratos TypeScript alineados con DTOs de C#
│   │   ├── client.ts         # Cliente HTTP con Bearer token y ProblemDetails
│   │   ├── mockData.ts       # Datos semilla realistas (6 meses, cuotas, fijos)
│   │   ├── mockService.ts    # Capa mock en memoria/localStorage con reglas
│   │   └── apiService.ts     # Gateway unificado (intercambiable Mock <-> Real)
│   ├── components/
│   │   ├── common/           # Skeletons, EmptyState, ErrorState, ConfirmDialog, FAB
│   │   ├── layout/           # Header, Navbar (móvil y escritorio), Layout
│   │   └── modals/           # PopupDeCarga, CategoryModal, TarjetaModal, AdelantarCuotasModal
│   ├── context/
│   │   └── ModalContext.tsx  # Control global del modal de alta/edición
│   ├── hooks/
│   │   ├── useAuth.ts        # Hook de autenticación y sesión
│   │   ├── useQueries.ts     # React Query hooks (dashboard, reportes, pendientes, etc.)
│   │   └── useMutations.ts   # Mutaciones con invalidación de caché automática
│   ├── pages/
│   │   ├── Login.tsx         # Pantalla de acceso
│   │   ├── Dashboard.tsx     # Dashboard del mes actual con 4 tarjetas y gráficos
│   │   ├── Reportes.tsx      # Reportes con filtros multi-categoría y movimientos
│   │   └── Pendientes.tsx    # Listado de pendientes y adelanto de cuotas
│   ├── styles/
│   │   └── tokens.css        # ARCHIVO ÚNICO DE TOKENS (Paleta Holst, radios, etc.)
│   ├── utils/
│   │   └── formatters.ts     # Formato de moneda ARS ($ 1.234,56) y fechas AR
│   ├── App.tsx               # Rutas protegidas y proveedores
│   └── main.tsx              # Punto de entrada de la aplicación
├── tailwind.config.js        # Configuración de Tailwind vinculada a tokens.css
└── README.md
```

---

## 5. Decisiones de Diseño y Aspectos Asumidos

1. **Orden de Pendientes**: La interfaz consume el endpoint `/api/v1/pendientes` sin aplicar ordenamientos adicionales en el cliente, respetando estrictamente la jerarquía enviada por la API (gastos eventuales pendientes primero por monto descendente, luego compras en cuotas).
2. **Vocabulario de Adelanto de Cuotas**: Cumpliendo con la especificación, **no se utiliza la palabra "Pagar"** en ninguna parte del flujo de cuotas; se utilizan términos como *"Adelantar"*, *"Adelantar cuotas"* y *"Confirmar adelanto"*.
3. **Ausencia de "Sobrante"**: El dashboard no incluye tarjetas ni cálculos de saldo remanente/sobrante, conforme a la regla de negocio explícita.
4. **Fechas en formato calendario**: Las fechas de movimientos se intercambian como cadenas `YYYY-MM-DD` sin transformaciones de zona horaria, previniendo discrepancias de día de vencimiento entre backend y cliente.
5. **Fuera de alcance respetado**: No se implementaron pantallas de menú general, gestión de tarjetas independiente, administración masiva de categorías ni auto-registro de usuarios, manteniéndose exclusivamente en el alcance asignado.

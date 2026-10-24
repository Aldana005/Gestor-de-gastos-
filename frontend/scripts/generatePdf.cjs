const fs = require('fs');
const path = require('path');
const PDFDocument = require('pdfkit');

function createDocumentationPdf(outputPath) {
  const doc = new PDFDocument({
    margin: 45,
    size: 'A4',
    bufferPages: true,
  });

  const writeStream = fs.createWriteStream(outputPath);
  doc.pipe(writeStream);

  // Paleta oficial Holst (Небо) y semánticos
  const colors = {
    brand900: '#04202E',
    brand800: '#0A3C55',
    brand700: '#0F5E83',
    brand600: '#1786B4',
    brand500: '#4FB3DA',
    brand300: '#98D5EE',
    brand100: '#E1F2FA',
    income: '#3DDC97',
    expense: '#FF6B6B',
    warning: '#FFC857',
    darkText: '#1A2A36',
    grayText: '#4A5568',
    cardBg: '#F0F7FB',
    cardBorder: '#CBE5F2',
  };

  // Helper para títulos de sección
  function drawSectionHeader(title) {
    doc.moveDown(0.8);
    const y = doc.y;
    doc.rect(45, y, 4, 18).fill(colors.brand600);
    doc
      .fontSize(14)
      .font('Helvetica-Bold')
      .fillColor(colors.brand800)
      .text(`  ${title}`, 52, y + 2);
    doc.moveDown(0.6);
  }

  // Helper para subtítulos
  function drawSubHeader(subtitle) {
    doc.moveDown(0.4);
    doc
      .fontSize(11)
      .font('Helvetica-Bold')
      .fillColor(colors.brand700)
      .text(subtitle);
    doc.moveDown(0.2);
  }

  // Helper para párrafo normal
  function drawParagraph(text) {
    doc
      .fontSize(9.5)
      .font('Helvetica')
      .fillColor(colors.darkText)
      .text(text, { align: 'justify', lineGap: 2 });
    doc.moveDown(0.3);
  }

  // Helper para viñetas (bullet points)
  function drawBullet(boldLabel, text) {
    const left = 55;
    doc
      .fontSize(9)
      .font('Helvetica-Bold')
      .fillColor(colors.brand800)
      .text(`• ${boldLabel}: `, left, doc.y, { continued: true })
      .font('Helvetica')
      .fillColor(colors.darkText)
      .text(text, { lineGap: 2 });
    doc.moveDown(0.2);
  }

  // Helper para cajas destacadas
  function drawCallout(title, text, color = colors.brand500, bgColor = colors.cardBg) {
    const y = doc.y;
    const width = doc.page.width - 90;
    const height = 45;
    doc.roundedRect(45, y, width, height, 6).fillAndStroke(bgColor, colors.cardBorder);
    doc
      .fontSize(9.5)
      .font('Helvetica-Bold')
      .fillColor(colors.brand800)
      .text(title, 55, y + 8);
    doc
      .fontSize(8.5)
      .font('Helvetica')
      .fillColor(colors.darkText)
      .text(text, 55, y + 23, { width: width - 20, lineGap: 1.5 });
    doc.y = y + height + 10;
  }

  // -------------------------------------------------------------
  // PORTADA / ENCABEZADO PRINCIPAL
  // -------------------------------------------------------------
  const headerHeight = 90;
  doc.rect(0, 0, doc.page.width, headerHeight).fill(colors.brand900);

  // Acento de color en degradé / barra inferior del header
  doc.rect(0, headerHeight - 4, doc.page.width, 4).fill(colors.brand500);

  doc
    .fontSize(24)
    .font('Helvetica-Bold')
    .fillColor(colors.brand100)
    .text('GASTITO — Frontend Web', 45, 22);

  doc
    .fontSize(10)
    .font('Helvetica')
    .fillColor(colors.brand300)
    .text('Documentación Técnica, Arquitectura y Manual de Integración', 45, 52);

  doc
    .fontSize(8.5)
    .font('Helvetica-Bold')
    .fillColor(colors.brand500)
    .text('Moneda: Pesos Argentinos (ARS) · Single-Tenant · Mobile-First', 45, 67);

  doc.y = headerHeight + 20;

  // -------------------------------------------------------------
  // 1. IDENTIFICACIÓN Y OBJETIVOS
  // -------------------------------------------------------------
  drawSectionHeader('1. Identificación y Objetivos del Frontend');
  drawParagraph(
    'GASTITO es una aplicación web mobile-first y responsive diseñada para registrar, organizar y proyectar ingresos, gastos personales y compras en cuotas, operando exclusivamente en pesos argentinos (ARS). El frontend fue construido desacoplado de la API REST .NET 8 mediante una capa de datos simulada (mock) intercambiable por configuración.'
  );

  drawBullet('Enfoque de diseño', 'Mobile-first adaptado para teléfonos Android e iOS (navegador web), y optimizado con layout adaptable para escritorio.');
  drawBullet('Estética de referencia', 'Inspirada en Nexora: degradé azul vertical, tarjetas con efecto vidrio suave (glassmorphism), píldoras para chips y filtros, gráfico de dona y barra de navegación inferior fija.');
  drawBullet('Paleta oficial', 'Sistema de color oficial "Небо" (Holst) de 7 tonalidades desde #04202E hasta #E1F2FA, con tokens centralizados.');

  // -------------------------------------------------------------
  // 2. SISTEMA DE DISEÑO Y TOKENS CENTRALIZADOS
  // -------------------------------------------------------------
  drawSectionHeader('2. Sistema de Diseño y Tokens Centralizados');
  drawParagraph(
    'Todos los valores de diseño residen en el archivo src/styles/tokens.css, que actúa como fuente única de verdad para Tailwind CSS y los estilos globales de la aplicación.'
  );

  // Tabla de paleta de colores
  const paletteTableY = doc.y;
  doc.roundedRect(45, paletteTableY, doc.page.width - 90, 75, 6).fillAndStroke(colors.cardBg, colors.cardBorder);
  
  doc.fontSize(8.5).font('Helvetica-Bold').fillColor(colors.brand800).text('Paleta Base Oficial (Holst):', 55, paletteTableY + 8);
  
  const swatches = [
    { code: '#04202E', label: 'Brand 900 (Fondo app)', col: colors.brand900, textCol: '#FFFFFF' },
    { code: '#0A3C55', label: 'Brand 800 (Fondo degradé)', col: colors.brand800, textCol: '#FFFFFF' },
    { code: '#0F5E83', label: 'Brand 700 (Vidrio tarjetas)', col: colors.brand700, textCol: '#FFFFFF' },
    { code: '#1786B4', label: 'Brand 600 (Acentos)', col: colors.brand600, textCol: '#FFFFFF' },
    { code: '#4FB3DA', label: 'Brand 500 (Acción principal)', col: colors.brand500, textCol: '#04202E' },
    { code: '#98D5EE', label: 'Brand 300 (Texto secundario)', col: colors.brand300, textCol: '#04202E' },
    { code: '#E1F2FA', label: 'Brand 100 (Texto principal)', col: colors.brand100, textCol: '#04202E' },
  ];

  let swatchX = 55;
  const swatchY = paletteTableY + 24;
  swatches.forEach((s) => {
    doc.roundedRect(swatchX, swatchY, 68, 26, 4).fill(s.col);
    doc.fontSize(7).font('Helvetica-Bold').fillColor(s.textCol).text(s.code, swatchX + 4, swatchY + 4, { width: 60, align: 'center' });
    doc.fontSize(5.5).font('Helvetica').fillColor(s.textCol).text(s.label.split(' ')[0], swatchX + 4, swatchY + 14, { width: 60, align: 'center' });
    swatchX += 71;
  });

  doc.fontSize(8).font('Helvetica-Bold').fillColor(colors.darkText).text('Colores Semánticos:', 55, paletteTableY + 56);
  doc.font('Helvetica').text('Ingreso (+): #3DDC97 (Verde Menta)  |  Gasto/Peligro (-): #FF6B6B (Coral)  |  Aviso/Cuotas: #FFC857 (Ámbar)', 145, paletteTableY + 56);

  doc.y = paletteTableY + 85;

  drawBullet('Efecto Vidrio', 'Tarjetas en rgba(15, 94, 131, 0.38) con borde al 20% rgba(152, 213, 238, 0.20) y backdrop-blur de 16px.');
  drawBullet('Botones Táctiles', 'Altura mínima de 44 px (touch target) garantizada en todas las acciones para celulares.');
  drawBullet('Tipografía', 'Familia Inter / sans-serif moderna con jerarquía de montos grandes en negrita.');

  // -------------------------------------------------------------
  // 3. ARQUITECTURA TÉCNICA Y ESTRUCTURA DE ARCHIVOS
  // -------------------------------------------------------------
  drawSectionHeader('3. Arquitectura Técnica y Estructura');
  drawParagraph(
    'El frontend está desacoplado mediante una arquitectura por capas basada en React 19, TypeScript, React Query y Tailwind CSS:'
  );

  drawBullet('src/api/types.ts', 'Contratos de datos de entrada/salida y enums que replican al 100% los DTOs de Gastito.Presentation.');
  drawBullet('src/api/mockData.ts', 'Dataset realista de 6 meses de historial, fijos de ingreso y gasto, 2 compras en cuotas con avances distintos y las 4 categorías protegidas del sistema.');
  drawBullet('src/api/mockService.ts', 'Motor mock persistido en localStorage que emula toda la lógica del backend (cálculo de cuotas, expansiones mensuales, filtros y adelantos).');
  drawBullet('src/api/apiService.ts', 'Gateway unificado que conmuta entre mockService y client.ts según la variable VITE_API_BASE_URL.');
  drawBullet('src/components/modals/', 'PopupDeCarga (alta y edición), AdelantarCuotasModal, CategoryModal y TarjetaModal.');
  drawBullet('src/pages/', 'Login, Dashboard (mes actual), Reportes (filtros y movimientos) y Pendientes (cuotas y deudas).');

  // Salto de página para la sección 4
  doc.addPage();

  // -------------------------------------------------------------
  // 4. MAPA DE PANTALLAS Y FLUJOS DE USUARIO
  // -------------------------------------------------------------
  drawSectionHeader('4. Mapa de Pantallas y Flujos de Usuario');

  drawSubHeader('A. Pantalla de Login');
  drawParagraph(
    'Permite la autenticación con las credenciales de desarrollo admin / admin. Almacena el JWT en localStorage y lo adjunta en Authorization: Bearer <token>. Dispone de redirección automática al login si cualquier endpoint devuelve 401 Unauthorized.'
  );

  drawSubHeader('B. Pantalla Principal (Dashboard — Todo del mes actual)');
  drawParagraph(
    'Diseñada en formato de tarjetas e interfaces gráficas con datos correspondientes al mes en curso:'
  );
  drawBullet('4 Tarjetas Principales', 'Total ingresos — Este mes (+), Total gastos — Este mes (-), Monto pendiente (con enlace a Pendientes) y Reportes (con enlace a Reportes).');
  drawBullet('Regla Crítica', 'No existe tarjeta ni concepto de "sobrante" ni remanente, conforme a la especificación.');
  drawBullet('Gastos por Categoría', 'Gráfico de dona (PieChart) interactivo que muestra únicamente las categorías con gastos en el mes actual.');
  drawBullet('Ingresos vs Gastos', 'Gráfico de líneas con 3 meses predeterminados y selector de filtro ("Meses") de 3 a 12 meses, afectando exclusivamente a este gráfico.');
  drawBullet('Próximas Deudas', 'Listado de compras en cuotas con cuotas sin pagar (ej. progreso 3/6, valor cuota) y botón "Ver más".');
  drawBullet('Botón Flotante (+)', 'Botón flotante fijado en la esquina inferior derecha para registro rápido de movimientos.');

  drawSubHeader('C. Popup de Carga ("+")');
  drawParagraph(
    'Modal a pantalla completa en móviles o ventana flotante en escritorio con selector Ingresos / Gastos:'
  );
  drawBullet('Flujo Ingreso', 'Monto (ARS), Categoría de ingreso (+ crear nueva), Frecuencia (Fijo / Eventual), Fecha (hoy por defecto), Fecha de fin (opcional, solo visible si es Fijo), Descripción y Notas.');
  drawBullet('Flujo Gasto', '1) Medio de pago (Tarjeta / Efectivo / Transferencia). 2) Selección de tarjeta (+ crear nueva con tipo crédito/débito). 3) Si es crédito: "¿Tiene cuotas?" Sí/No. Si es Sí: monto total, cantidad de cuotas (mín. 2) y fecha primera cuota con calendario. Con cuotas se oculta el campo de monto simple. 4) Frecuencia Fijo / Eventual (las cuotas bloquean la frecuencia fija). 5) Monto, Categoría (+ crear nueva), Fecha, Descripción y Notas.');

  drawSubHeader('D. Pantalla de Reportes y Últimos Movimientos');
  drawBullet('Filtros Reactivos', 'Selección múltiple de categorías (chips pill) y rango de fechas (desde / hasta con atajos rápidos de mes actual, 3 meses y año 2026). Cada cambio actualiza los datos en tiempo real.');
  drawBullet('Totales y Dona', 'Tarjetas de Total ingresos del período, Total gastos del período y gráfico de dona de categorías.');
  drawBullet('Últimos Movimientos', 'Lista paginada con acciones de Editar y Borrar (con diálogo de confirmación accesible). Los movimientos fijos se expanden mensualmente.');
  drawBullet('Próximas Deudas', 'Proyección de cuotas del período con acceso a Pendientes.');

  drawSubHeader('E. Pantalla de Pendientes');
  drawBullet('Orden de la API', 'Respeta el orden estricto de la API (gastos eventuales sin cuotas primero por monto desc, luego compras en cuotas).');
  drawBullet('Adelanto de Cuotas', 'Botón "Adelantar" que abre diálogo para elegir cuántas cuotas (mínimo 1, máximo cuotas restantes). NUNCA se utiliza la palabra "Pagar".');
  drawBullet('Historial de Adelantos', 'Visualización detallada de pagos de cuotas previos (rango "cuotas X a Y", monto y fecha).');
  drawBullet('Filtros', 'Filtros desplegables por categoría, rango de fechas y rango de monto (mínimo y máximo).');

  // Salto de página para la sección 5
  doc.addPage();

  // -------------------------------------------------------------
  // 5. REGLAS DE NEGOCIO Y COMPORTAMIENTO DEL CLIENTE
  // -------------------------------------------------------------
  drawSectionHeader('5. Reglas de Negocio y Validaciones Especiales');

  drawSubHeader('1. Edición de Movimientos Fijos con Cambio de Monto');
  drawParagraph(
    'Al editar un movimiento recurrente (Fijo) y modificar su monto, la interfaz solicita al usuario seleccionar entre "Desde este mes" o "Desde el mes siguiente". Esta selección se envía en la petición PUT como aplicarDesde = 1 (MesActual) o 2 (MesSiguiente), preservando intactos los valores de los meses históricos anteriores.'
  );

  drawSubHeader('2. Protección de Compras en Cuotas con Pagos Previos');
  drawParagraph(
    'Si una compra en cuotas ya tiene cuotas pagadas o adelantadas (cuotasPagadas > 0), el frontend bloquea la edición del monto total y de la cantidad de cuotas, permitiendo únicamente editar la descripción, categoría y notas adicionales.'
  );

  drawSubHeader('3. Formatos Estrictos de Presentación');
  drawBullet('Moneda', 'Pesos Argentinos con separador de miles y decimales: $ 1.234,56.');
  drawBullet('Fechas en pantalla', 'Formato argentino tradicional: dd/mm/aaaa.');
  drawBullet('Fechas enviadas a la API', 'Día calendario limpio YYYY-MM-DD sin alteraciones por huso horario.');

  drawSubHeader('4. Estados de Interfaz en Cada Pantalla');
  drawBullet('Carga (Loading)', 'Esqueletos con animación shimmer en tarjetas y listas.');
  drawBullet('Vacío (Empty)', 'Mensajes explicativos con icono y botón de acción.');
  drawBullet('Error', 'Mensaje de fallo con botón de reintento para garantizar resiliencia.');

  // -------------------------------------------------------------
  // 6. CONTRATO DE API REST V1 (NET 8)
  // -------------------------------------------------------------
  drawSectionHeader('6. Contrato de API REST v1 (.NET 8)');
  drawParagraph(
    'La capa de servicios interactúa con los siguientes endpoints en /api/v1 (manejados vía Bearer JWT):'
  );

  const endpoints = [
    { method: 'POST', path: '/auth/login', desc: 'Login admin/admin, devuelve token JWT y expiraEnUtc.' },
    { method: 'GET / POST / PUT / DELETE', path: '/categorias', desc: 'CRUD de categorías (las 4 del sistema no se editan ni borran).' },
    { method: 'GET / POST / PUT / DELETE', path: '/tarjetas', desc: 'CRUD de tarjetas de crédito y débito.' },
    { method: 'GET', path: '/movimientos', desc: 'Listado paginado con filtros de fecha, tipo y categoriaIds.' },
    { method: 'POST / PUT / DELETE', path: '/movimientos/{id}', desc: 'Creación, actualización (con aplicarDesde) y borrado lógico.' },
    { method: 'POST', path: '/movimientos/{id}/cuotas/adelantar', desc: 'Adelanto de N cuotas (registra en PagoCuota).' },
    { method: 'GET', path: '/movimientos/{id}/cuotas/pagos', desc: 'Historial de pagos y adelantos de la compra.' },
    { method: 'GET', path: '/pendientes', desc: 'Gastos eventuales pendientes y compras en cuotas activas.' },
    { method: 'GET', path: '/dashboard?meses=3', desc: 'Totales del mes actual, torta por categoría, líneas y próximas deudas.' },
    { method: 'GET', path: '/reportes/resumen', desc: 'Resumen consolidado para rango de fechas y categorías.' },
  ];

  endpoints.forEach((ep) => {
    doc.fontSize(8).font('Helvetica-Bold').fillColor(colors.brand600).text(ep.method, 55, doc.y, { continued: true });
    doc.font('Helvetica-Bold').fillColor(colors.brand800).text(`  ${ep.path}: `, { continued: true });
    doc.font('Helvetica').fillColor(colors.darkText).text(ep.desc);
    doc.moveDown(0.2);
  });

  // -------------------------------------------------------------
  // 7. GUÍA DE INSTALACIÓN Y PUESTA EN MARCHA
  // -------------------------------------------------------------
  drawSectionHeader('7. Guía de Ejecución y Conmutación de Entorno');

  drawCallout(
    'Ejecutar con Capa Mock (Por Defecto):',
    '1. cd frontend\n2. npm install --legacy-peer-deps\n3. npm run dev\nLa aplicación iniciará en http://localhost:5173 con persistencia en localStorage y botón de reinicio demo.'
  );

  drawCallout(
    'Conectar a la API Real de .NET 8:',
    'En frontend/.env configurar:\nVITE_API_BASE_URL=http://localhost:5000/api/v1\nVITE_USE_MOCKS=false\nAl guardar y reiniciar (npm run dev), todas las peticiones irán al backend real.',
    colors.income
  );

  // -------------------------------------------------------------
  // 8. ASPECTOS ASUMIDOS Y FUERA DE ALCANCE
  // -------------------------------------------------------------
  drawSectionHeader('8. Aspectos Asumidos y Fuera de Alcance');
  drawBullet('Fuera de alcance respetado', 'No se incluyeron pantallas de menú general, gestión masiva de categorías, abm independiente de tarjetas ni registro de usuarios.');
  drawBullet('Adelanto de cuotas', 'Nunca se utiliza el término "Pagar"; se emplean "Adelantar", "Adelantar cuotas" y "Confirmar adelanto".');
  drawBullet('Sobrante', 'No se calculó ni mostró indicador de sobrante.');

  // -------------------------------------------------------------
  // NUMERACIÓN DE PÁGINAS Y PIE DE PÁGINA
  // -------------------------------------------------------------
  const range = doc.bufferedPageRange();
  for (let i = range.start; i < range.start + range.count; i++) {
    doc.switchToPage(i);
    const bottomY = doc.page.height - 35;
    doc
      .fontSize(7.5)
      .font('Helvetica')
      .fillColor(colors.grayText)
      .text('GASTITO — Frontend Web · Documentación Oficial del Proyecto', 45, bottomY);
    doc
      .fontSize(7.5)
      .font('Helvetica-Bold')
      .fillColor(colors.brand700)
      .text(`Página ${i + 1} de ${range.count}`, doc.page.width - 120, bottomY, {
        align: 'right',
        width: 75,
      });
  }

  doc.end();

  return new Promise((resolve, reject) => {
    writeStream.on('finish', resolve);
    writeStream.on('error', reject);
  });
}

// Generar PDF en la raíz del proyecto y dentro de frontend/
const rootPdfPath = path.resolve(__dirname, '../../GASTITO_FRONTEND_DOCUMENTACION.pdf');
const frontendPdfPath = path.resolve(__dirname, '../DOCUMENTACION_FRONTEND.pdf');

console.log('Generando PDF en:', rootPdfPath);
createDocumentationPdf(rootPdfPath)
  .then(() => {
    console.log('PDF generado exitosamente en raíz.');
    fs.copyFileSync(rootPdfPath, frontendPdfPath);
    console.log('PDF copiado a frontend:', frontendPdfPath);
  })
  .catch((err) => {
    console.error('Error generando PDF:', err);
    process.exit(1);
  });

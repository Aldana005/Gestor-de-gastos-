/**
 * Formateadores de Moneda (ARS) y Fechas para GASTITO
 * - Moneda: Pesos Argentinos ($ 1.234,56)
 * - Fechas: Formato argentino dd/mm/aaaa
 * - Fechas de API: YYYY-MM-DD (día calendario sin desfase horario)
 */

/**
 * Formatea un número como pesos argentinos: $ 1.234,56
 */
export function formatCurrencyARS(value: number | string | null | undefined): string {
  if (value === null || value === undefined || value === '') return '$ 0,00';
  const num = typeof value === 'string' ? parseFloat(value) : value;
  if (isNaN(num)) return '$ 0,00';

  const parts = Math.abs(num).toFixed(2).split('.');
  const integerPart = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  const decimalPart = parts[1];
  const sign = num < 0 ? '-' : '';

  return `${sign}$ ${integerPart},${decimalPart}`;
}

/**
 * Formatea una fecha o string ISO a formato argentino: dd/mm/aaaa
 */
export function formatDateAR(dateInput: string | Date | null | undefined): string {
  if (!dateInput) return '-';

  if (typeof dateInput === 'string') {
    // Si viene como YYYY-MM-DD
    const match = dateInput.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (match) {
      const [, y, m, d] = match;
      return `${d}/${m}/${y}`;
    }
  }

  const d = new Date(dateInput);
  if (isNaN(d.getTime())) return '-';

  // Usar UTC si viene en ISO para evitar saltos de día por zona horaria
  const day = String(d.getUTCDate()).padStart(2, '0');
  const month = String(d.getUTCMonth() + 1).padStart(2, '0');
  const year = d.getUTCFullYear();

  return `${day}/${month}/${year}`;
}

/**
 * Devuelve la fecha de hoy en formato calendario YYYY-MM-DD (hora argentina)
 */
export function getTodayCalendarString(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/**
 * Convierte Date o string a YYYY-MM-DD limpio sin desfase
 */
export function toCalendarDateString(dateInput: string | Date): string {
  if (typeof dateInput === 'string') {
    const match = dateInput.match(/^(\d{4}-\d{2}-\d{2})/);
    if (match) return match[1];
    const d = new Date(dateInput);
    if (!isNaN(d.getTime())) {
      const year = d.getUTCFullYear();
      const month = String(d.getUTCMonth() + 1).padStart(2, '0');
      const day = String(d.getUTCDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    }
    return dateInput;
  }
  const year = dateInput.getFullYear();
  const month = String(dateInput.getMonth() + 1).padStart(2, '0');
  const day = String(dateInput.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/**
 * Formato de mes legible en español: "Octubre 2026"
 */
export function formatMonthYearAR(periodo: string | Date): string {
  let year: number;
  let month: number;

  if (typeof periodo === 'string') {
    const match = periodo.match(/^(\d{4})-(\d{2})/);
    if (match) {
      year = parseInt(match[1], 10);
      month = parseInt(match[2], 10) - 1;
    } else {
      const d = new Date(periodo);
      year = d.getFullYear();
      month = d.getMonth();
    }
  } else {
    year = periodo.getFullYear();
    month = periodo.getMonth();
  }

  const meses = [
    'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
    'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'
  ];

  return `${meses[month] || ''} ${year}`;
}

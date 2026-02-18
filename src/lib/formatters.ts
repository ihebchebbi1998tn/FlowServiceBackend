/**
 * Format a numeric value for display in stats cards
 * Returns '-' for zero/null/undefined values, otherwise returns the value
 */
export function formatStatValue(value: number | string | null | undefined): string | number {
  if (value === null || value === undefined) return '-';
  if (typeof value === 'number' && value === 0) return '-';
  if (typeof value === 'string') {
    // Check if it's a currency string starting with 0
    const numericPart = parseFloat(value.replace(/[^\\d.-]/g, ''));
    if (!isNaN(numericPart) && numericPart === 0) {
      // Keep the currency format but replace the number with '-'
      return value.replace(/[\d,]+/, '-');
    }
  }
  return value;
}

/**
 * Format currency value, returning '-' for zero
 */
export function formatCurrencyValue(value: number | null | undefined, currency = 'TND'): string {
  if (value === null || value === undefined || value === 0) return `-`;
  return `${value.toLocaleString()} ${currency}`;
}

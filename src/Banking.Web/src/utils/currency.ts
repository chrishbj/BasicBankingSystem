function normalizeCurrency(currency?: string) {
  const value = currency?.trim().toUpperCase()
  if (!value || value === 'USD' || value === 'US$' || value === '$' || value === 'CNY' || value === 'RMB') {
    return 'USD'
  }

  return value
}

export function formatCurrency(amount: number, currency = 'USD') {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: normalizeCurrency(currency),
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount)
}

export function formatCurrencyWithCode(amount: number, currency = 'USD') {
  return `${formatCurrency(amount, currency)} USD`
}

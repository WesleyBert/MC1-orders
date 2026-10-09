const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

const dateTime = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

const time = new Intl.DateTimeFormat('pt-BR', { timeStyle: 'medium' })

const integer = new Intl.NumberFormat('pt-BR')

const amountInput = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

export const formatCurrency = (value: number) => currency.format(value)

export const formatDateTime = (iso: string) => dateTime.format(new Date(iso))

export const formatTime = (date: Date | number) => time.format(date)

export const formatInteger = (value: number) => integer.format(value)

export const formatOrderNumber = (value: number) => `#${value}`

export const formatAmountInput = (value: number) => amountInput.format(value)

const MAX_AMOUNT_DIGITS = 11

export function maskAmountInput(input: string) {
  const digits = input.replace(/\D/g, '').replace(/^0+/, '').slice(0, MAX_AMOUNT_DIGITS)
  return digits === '' ? '' : amountInput.format(Number(digits) / 100)
}

export function normalizeAmount(input: string): string | null {
  let value = input.trim().replace(/^R\$/, '').replace(/\s/g, '')

  if (value.includes(',')) {
    value = value.replace(/\./g, '').replace(',', '.')
  } else if (/^\d{1,3}(\.\d{3})+$/.test(value)) {
    value = value.replace(/\./g, '')
  }

  return /^\d+(\.\d+)?$/.test(value) ? value : null
}

export function parseAmount(input: string): number | null {
  const normalized = normalizeAmount(input)
  return normalized === null ? null : Number(normalized)
}

export function hasAtMostTwoDecimals(input: string) {
  const normalized = normalizeAmount(input)
  return normalized !== null && /^\d+(\.\d{1,2})?$/.test(normalized)
}

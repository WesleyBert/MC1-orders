import { formatCurrency, hasAtMostTwoDecimals, maskAmountInput, parseAmount } from '@/lib/format'

describe('parseAmount', () => {
  it.each([
    ['1520,50', 1520.5],
    ['1.520,50', 1520.5],
    ['R$ 1.520,50', 1520.5],
    ['1520.50', 1520.5],
    ['1520', 1520],
    ['1.520', 1520],
    ['1.000.000', 1000000],
    ['0,5', 0.5],
    ['  99,9  ', 99.9],
  ])('converte "%s" em %d', (input, expected) => {
    expect(parseAmount(input)).toBe(expected)
  })

  it.each(['', 'abc', '1,2,3', '-10', '10,'])('rejeita "%s"', (input) => {
    expect(parseAmount(input)).toBeNull()
  })
})

describe('hasAtMostTwoDecimals', () => {
  it('aceita até duas casas e recusa três', () => {
    expect(hasAtMostTwoDecimals('10,99')).toBe(true)
    expect(hasAtMostTwoDecimals('10,999')).toBe(false)
  })
})

describe('formatCurrency', () => {
  it('formata em reais', () => {
    expect(formatCurrency(1520.5).replace(/\s/g, ' ')).toBe('R$ 1.520,50')
  })
})

describe('maskAmountInput', () => {
  it.each([
    ['1', '0,01'],
    ['15', '0,15'],
    ['1520', '15,20'],
    ['100000', '1.000,00'],
    ['1.520,5', '152,05'],
    ['00012', '0,12'],
    ['R$ 1a2b3', '1,23'],
    ['', ''],
    ['abc', ''],
    ['000', ''],
    ['999999999999999', '999.999.999,99'],
  ])('formata "%s" como "%s"', (input, expected) => {
    expect(maskAmountInput(input)).toBe(expected)
  })

  it('apagar o último caractere remove o último dígito', () => {
    expect(maskAmountInput('1.000,0')).toBe('100,00')
  })
})

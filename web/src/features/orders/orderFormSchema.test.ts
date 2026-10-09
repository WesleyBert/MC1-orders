import { orderFormSchema, type OrderFormValues } from './orderFormSchema'

const valid: OrderFormValues = {
  customerName: 'Maria Souza',
  description: 'Reposição de gôndola',
  totalAmount: '1.520,50',
  status: 'Open',
}

function firstError(values: Partial<OrderFormValues>, field: keyof OrderFormValues) {
  const result = orderFormSchema.safeParse({ ...valid, ...values })
  return result.success ? undefined : result.error.issues.find((issue) => issue.path[0] === field)?.message
}

describe('orderFormSchema', () => {
  it('aceita um pedido válido', () => {
    expect(orderFormSchema.safeParse(valid).success).toBe(true)
  })

  it.each([
    ['customerName', '   ', 'O nome do cliente é obrigatório.'],
    ['customerName', 'A', 'O nome do cliente deve ter entre 2 e 150 caracteres.'],
    ['description', '', 'A descrição é obrigatória.'],
    ['description', 'ab', 'A descrição deve ter entre 3 e 500 caracteres.'],
    ['totalAmount', '', 'O valor total é obrigatório.'],
    ['totalAmount', 'abc', 'Informe um valor válido, por exemplo 1.520,50.'],
    ['totalAmount', '0', 'O valor total deve ser maior que zero.'],
    ['totalAmount', '10,999', 'O valor total deve ter no máximo 2 casas decimais.'],
    ['totalAmount', '1.000.000.000', 'O valor total deve ser no máximo R$ 999.999.999,99.'],
  ] as const)('valida %s = "%s"', (field, value, message) => {
    expect(firstError({ [field]: value }, field)).toBe(message)
  })
})

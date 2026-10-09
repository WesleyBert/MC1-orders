import { z } from 'zod'
import { hasAtMostTwoDecimals, parseAmount } from '@/lib/format'

const MAX_AMOUNT = 999_999_999.99

const customerNameLength = 'O nome do cliente deve ter entre 2 e 150 caracteres.'
const descriptionLength = 'A descrição deve ter entre 3 e 500 caracteres.'

export const orderFormSchema = z.object({
  customerName: z
    .string()
    .trim()
    .min(1, 'O nome do cliente é obrigatório.')
    .min(2, customerNameLength)
    .max(150, customerNameLength),
  description: z
    .string()
    .trim()
    .min(1, 'A descrição é obrigatória.')
    .min(3, descriptionLength)
    .max(500, descriptionLength),
  totalAmount: z
    .string()
    .trim()
    .min(1, 'O valor total é obrigatório.')
    .refine((value) => parseAmount(value) !== null, 'Informe um valor válido, por exemplo 1.520,50.')
    .refine((value) => (parseAmount(value) ?? 1) > 0, 'O valor total deve ser maior que zero.')
    .refine((value) => (parseAmount(value) ?? 0) <= MAX_AMOUNT, 'O valor total deve ser no máximo R$ 999.999.999,99.')
    .refine((value) => parseAmount(value) === null || hasAtMostTwoDecimals(value), 'O valor total deve ter no máximo 2 casas decimais.'),
  status: z.enum(['Open', 'Paid', 'Cancelled']),
})

export type OrderFormValues = z.input<typeof orderFormSchema>

export const ORDER_FORM_FIELDS = ['customerName', 'description', 'totalAmount', 'status'] as const

export type OrderFormField = (typeof ORDER_FORM_FIELDS)[number]

export const isOrderFormField = (field: string): field is OrderFormField =>
  (ORDER_FORM_FIELDS as readonly string[]).includes(field)

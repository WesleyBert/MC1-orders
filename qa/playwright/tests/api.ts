import type { APIRequestContext } from '@playwright/test'

export interface CreatedOrder {
  id: string
  number: number
  customerName: string
  version: number
}

export function uniqueName(prefix: string) {
  return `${prefix} ${Date.now().toString().slice(-6)}`
}

export async function createOrder(request: APIRequestContext, customerName: string, totalAmount = 250.75) {
  const response = await request.post('/api/v1/orders', {
    data: { customerName, description: 'Pedido criado pelo teste automatizado', totalAmount },
  })
  if (response.status() !== 201) {
    throw new Error(`Falha ao criar pedido: ${response.status()}`)
  }
  return (await response.json()) as CreatedOrder
}

export async function payOrder(request: APIRequestContext, order: CreatedOrder) {
  const response = await request.put(`/api/v1/orders/${order.id}`, {
    headers: { 'If-Match': `"${order.version}"` },
    data: {
      customerName: order.customerName,
      description: 'Pedido pago pelo teste automatizado',
      totalAmount: 250.75,
      status: 'Paid',
    },
  })
  if (response.status() !== 200) {
    throw new Error(`Falha ao pagar pedido: ${response.status()}`)
  }
}

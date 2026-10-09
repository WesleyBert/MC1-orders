import type { OrderStatus } from '@/api/types'

export const ORDER_STATUSES: readonly OrderStatus[] = ['Open', 'Paid', 'Cancelled']

export const STATUS_LABELS: Record<OrderStatus, string> = {
  Open: 'Aberto',
  Paid: 'Pago',
  Cancelled: 'Cancelado',
}

export const STATUS_STYLES: Record<OrderStatus, string> = {
  Open: 'bg-blue-100 text-blue-800',
  Paid: 'bg-green-100 text-green-800',
  Cancelled: 'bg-zinc-200 text-zinc-700',
}

const ALLOWED_TARGETS: Record<OrderStatus, readonly OrderStatus[]> = {
  Open: ['Open', 'Paid', 'Cancelled'],
  Paid: [],
  Cancelled: [],
}

export const allowedTargets = (status: OrderStatus) => ALLOWED_TARGETS[status]

export const isFinal = (status: OrderStatus) => ALLOWED_TARGETS[status].length === 0

export const canDelete = (status: OrderStatus) => status !== 'Paid'

export const immutableMessage = (status: OrderStatus) =>
  `Pedidos com status ${STATUS_LABELS[status]} não podem ser alterados.`

import type { OrderStatus } from './types'

export const ORDER_STATUSES: readonly OrderStatus[] = ['Open', 'Paid', 'Cancelled']

export const STATUS_LABELS: Record<OrderStatus, string> = {
  Open: 'Aberto',
  Paid: 'Pago',
  Cancelled: 'Cancelado',
}

export const STATUS_STYLES: Record<OrderStatus, { badge: string; dot: string }> = {
  Open: { badge: 'bg-blue-50 text-blue-700 ring-blue-600/20', dot: 'bg-blue-500' },
  Paid: { badge: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20', dot: 'bg-emerald-500' },
  Cancelled: { badge: 'bg-zinc-100 text-zinc-600 ring-zinc-500/20', dot: 'bg-zinc-400' },
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

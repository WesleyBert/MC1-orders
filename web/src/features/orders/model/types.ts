export type OrderStatus = 'Open' | 'Paid' | 'Cancelled'

export type SortField = 'createdAt' | 'number' | 'customerName' | 'totalAmount' | 'status'

export type SortDirection = 'asc' | 'desc'

export interface Order {
  id: string
  number: number
  customerName: string
  description: string
  totalAmount: number
  status: OrderStatus
  createdAt: string
  updatedAt: string
  version: number
}

export interface ListOrdersParams {
  search?: string
  status?: OrderStatus
  page: number
  pageSize: number
  sortBy: SortField
  sortDir: SortDirection
}

export interface CreateOrderInput {
  customerName: string
  description: string
  totalAmount: number
}

export interface UpdateOrderInput extends CreateOrderInput {
  status: OrderStatus
}

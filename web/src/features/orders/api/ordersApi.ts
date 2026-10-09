import { request, toETag } from '@/lib/http/client'
import type { PagedResponse } from '@/lib/http/types'
import type { CreateOrderInput, ListOrdersParams, Order, OrdersSummary, UpdateOrderInput } from '../model/types'

const BASE = '/api/v1/orders'

export async function listOrders(params: ListOrdersParams, signal?: AbortSignal) {
  const query = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize),
    sortBy: params.sortBy,
    sortDir: params.sortDir,
  })
  if (params.search) {
    query.set('search', params.search)
  }
  if (params.status) {
    query.set('status', params.status)
  }

  const { data } = await request<PagedResponse<Order>>(`${BASE}?${query}`, { signal })
  return data
}

export async function getOrdersSummary(signal?: AbortSignal) {
  const { data } = await request<OrdersSummary>(`${BASE}/summary`, { signal })
  return data
}

export async function getOrder(id: string, signal?: AbortSignal) {
  const { data } = await request<Order>(`${BASE}/${id}`, { signal })
  return data
}

export async function createOrder(input: CreateOrderInput) {
  const { data } = await request<Order>(BASE, { method: 'POST', body: input })
  return data
}

export async function updateOrder(id: string, version: number, input: UpdateOrderInput) {
  const { data } = await request<Order>(`${BASE}/${id}`, {
    method: 'PUT',
    body: input,
    ifMatch: toETag(version),
  })
  return data
}

export async function deleteOrder(id: string, version: number) {
  await request<void>(`${BASE}/${id}`, { method: 'DELETE', ifMatch: toETag(version) })
}

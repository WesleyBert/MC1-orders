import { useCallback, useEffect, useState } from 'react'
import type { OrderStatus, SortDirection, SortField } from '../model/types'
import { ORDER_STATUSES } from '../model/orderStatus'

export const PAGE_SIZES = [10, 20, 50] as const

const SORT_FIELDS: readonly SortField[] = ['createdAt', 'number', 'customerName', 'totalAmount', 'status']

export interface OrdersUrlState {
  search: string
  status: OrderStatus | undefined
  page: number
  pageSize: number
  sortBy: SortField
  sortDir: SortDirection
}

const DEFAULTS: OrdersUrlState = {
  search: '',
  status: undefined,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAt',
  sortDir: 'desc',
}

function readState(): OrdersUrlState {
  const params = new URLSearchParams(window.location.search)
  const status = params.get('status')
  const sort = params.get('sort')
  const dir = params.get('dir')
  const page = Number(params.get('page'))
  const size = Number(params.get('size'))

  return {
    search: params.get('q') ?? DEFAULTS.search,
    status: ORDER_STATUSES.find((s) => s === status),
    page: Number.isInteger(page) && page > 0 ? page : DEFAULTS.page,
    pageSize: PAGE_SIZES.find((s) => s === size) ?? DEFAULTS.pageSize,
    sortBy: SORT_FIELDS.find((f) => f === sort) ?? DEFAULTS.sortBy,
    sortDir: dir === 'asc' || dir === 'desc' ? dir : DEFAULTS.sortDir,
  }
}

function writeState(state: OrdersUrlState) {
  const params = new URLSearchParams()
  if (state.search) params.set('q', state.search)
  if (state.status) params.set('status', state.status)
  if (state.page !== DEFAULTS.page) params.set('page', String(state.page))
  if (state.pageSize !== DEFAULTS.pageSize) params.set('size', String(state.pageSize))
  if (state.sortBy !== DEFAULTS.sortBy) params.set('sort', state.sortBy)
  if (state.sortDir !== DEFAULTS.sortDir) params.set('dir', state.sortDir)

  const query = params.toString()
  window.history.replaceState(null, '', query ? `?${query}` : window.location.pathname)
}

export function useOrdersUrlState() {
  const [state, setState] = useState(readState)

  useEffect(() => {
    writeState(state)
  }, [state])

  useEffect(() => {
    const onPopState = () => setState(readState())
    window.addEventListener('popstate', onPopState)
    return () => window.removeEventListener('popstate', onPopState)
  }, [])

  const update = useCallback((changes: Partial<OrdersUrlState>) => {
    setState((current) => {
      const resetsPage = ['search', 'status', 'pageSize', 'sortBy', 'sortDir'].some((key) => key in changes)
      return { ...current, ...(resetsPage ? { page: 1 } : {}), ...changes }
    })
  }, [])

  const reset = useCallback(() => setState({ ...DEFAULTS, pageSize: state.pageSize }), [state.pageSize])

  return { state, update, reset }
}

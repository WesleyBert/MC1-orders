import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createOrder, deleteOrder, getOrder, listOrders, updateOrder } from '@/api/orders'
import type { CreateOrderInput, ListOrdersParams, Order, UpdateOrderInput } from '@/api/types'

export const REFRESH_INTERVAL_MS = 10_000

export const orderKeys = {
  all: ['orders'] as const,
  lists: () => [...orderKeys.all, 'list'] as const,
  list: (params: ListOrdersParams) => [...orderKeys.lists(), params] as const,
  detail: (id: string) => [...orderKeys.all, 'detail', id] as const,
}

export function useOrdersList(params: ListOrdersParams) {
  return useQuery({
    queryKey: orderKeys.list(params),
    queryFn: ({ signal }) => listOrders(params, signal),
    placeholderData: keepPreviousData,
    refetchInterval: REFRESH_INTERVAL_MS,
  })
}

export function useOrder(id: string | undefined) {
  return useQuery({
    queryKey: orderKeys.detail(id ?? ''),
    queryFn: ({ signal }) => getOrder(id!, signal),
    enabled: id !== undefined,
    staleTime: 0,
    refetchOnWindowFocus: false,
    retry: false,
  })
}

function useOrderCacheSync() {
  const queryClient = useQueryClient()
  return {
    saved: (order: Order) => {
      queryClient.setQueryData(orderKeys.detail(order.id), order)
      return queryClient.invalidateQueries({ queryKey: orderKeys.lists() })
    },
    removed: (id: string) => {
      queryClient.removeQueries({ queryKey: orderKeys.detail(id) })
      return queryClient.invalidateQueries({ queryKey: orderKeys.lists() })
    },
  }
}

export function useCreateOrder() {
  const sync = useOrderCacheSync()
  return useMutation({
    mutationFn: (input: CreateOrderInput) => createOrder(input),
    onSuccess: sync.saved,
  })
}

export function useUpdateOrder() {
  const sync = useOrderCacheSync()
  return useMutation({
    mutationFn: ({ order, input }: { order: Order; input: UpdateOrderInput }) =>
      updateOrder(order.id, order.version, input),
    onSuccess: sync.saved,
  })
}

export function useDeleteOrder() {
  const sync = useOrderCacheSync()
  return useMutation({
    mutationFn: (order: Order) => deleteOrder(order.id, order.version),
    onSettled: (_data, _error, order) => sync.removed(order.id),
  })
}

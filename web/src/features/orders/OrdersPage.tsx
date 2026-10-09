import { useCallback, useEffect, useState } from 'react'
import { PackageSearchIcon, TriangleAlertIcon } from 'lucide-react'
import type { Order, SortField } from '@/api/types'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { errorMessage } from '@/lib/errors'
import { DeleteOrderDialog } from './DeleteOrderDialog'
import { OrderFormDialog, type OrderDialogState } from './OrderFormDialog'
import { OrdersPagination } from './OrdersPagination'
import { OrdersTable } from './OrdersTable'
import { OrdersToolbar } from './OrdersToolbar'
import { RefreshIndicator } from './RefreshIndicator'
import { useOrdersList } from './queries'
import { useOrdersUrlState } from './useOrdersUrlState'

const DEFAULT_DIRECTION: Record<SortField, 'asc' | 'desc'> = {
  createdAt: 'desc',
  number: 'desc',
  customerName: 'asc',
  totalAmount: 'desc',
  status: 'asc',
}

export function OrdersPage() {
  const { state, update, reset } = useOrdersUrlState()
  const [dialog, setDialog] = useState<OrderDialogState | null>(null)
  const [orderToDelete, setOrderToDelete] = useState<Order | null>(null)

  const ordersQuery = useOrdersList({
    search: state.search || undefined,
    status: state.status,
    page: state.page,
    pageSize: state.pageSize,
    sortBy: state.sortBy,
    sortDir: state.sortDir,
  })
  const data = ordersQuery.data

  useEffect(() => {
    if (data && data.totalPages > 0 && state.page > data.totalPages) {
      update({ page: data.totalPages })
    }
  }, [data, state.page, update])

  const onSearchChange = useCallback((search: string) => update({ search }), [update])

  const onSort = (field: SortField) =>
    update({
      sortBy: field,
      sortDir: field === state.sortBy ? (state.sortDir === 'asc' ? 'desc' : 'asc') : DEFAULT_DIRECTION[field],
    })

  const hasFilters = state.search !== '' || state.status !== undefined

  return (
    <div className="mx-auto flex min-h-svh max-w-7xl flex-col gap-6 px-4 py-8 sm:px-6">
      <header className="flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Pedidos</h1>
          <p className="text-sm text-muted-foreground">Cadastro e acompanhamento de pedidos.</p>
        </div>
        <RefreshIndicator
          updatedAt={ordersQuery.dataUpdatedAt}
          isFetching={ordersQuery.isFetching}
          failed={ordersQuery.isError && data !== undefined}
        />
      </header>

      <OrdersToolbar
        search={state.search}
        status={state.status}
        onSearchChange={onSearchChange}
        onStatusChange={(status) => update({ status })}
        onCreate={() => setDialog({ mode: 'create' })}
      />

      <section className="overflow-hidden rounded-xl border bg-card" aria-label="Lista de pedidos">
        {ordersQuery.isPending ? (
          <LoadingRows />
        ) : ordersQuery.isError && !data ? (
          <EmptyState
            icon={<TriangleAlertIcon className="size-8 text-destructive" />}
            title="Não foi possível carregar os pedidos."
            description={errorMessage(ordersQuery.error)}
            action={<Button onClick={() => ordersQuery.refetch()}>Tentar novamente</Button>}
          />
        ) : data && data.items.length === 0 ? (
          <EmptyState
            icon={<PackageSearchIcon className="size-8 text-muted-foreground" />}
            title={hasFilters ? 'Nenhum pedido encontrado para os filtros aplicados.' : 'Nenhum pedido cadastrado.'}
            action={
              hasFilters ? (
                <Button variant="outline" onClick={reset}>
                  Limpar filtros
                </Button>
              ) : (
                <Button onClick={() => setDialog({ mode: 'create' })}>Novo pedido</Button>
              )
            }
          />
        ) : (
          data && (
            <div className="overflow-x-auto">
              <OrdersTable
                orders={data.items}
                sortBy={state.sortBy}
                sortDir={state.sortDir}
                onSort={onSort}
                onEdit={(order) => setDialog({ mode: 'edit', orderId: order.id })}
                onDelete={setOrderToDelete}
              />
            </div>
          )
        )}
      </section>

      {data && data.totalItems > 0 && (
        <OrdersPagination
          page={Math.min(state.page, Math.max(data.totalPages, 1))}
          pageSize={state.pageSize}
          totalItems={data.totalItems}
          totalPages={data.totalPages}
          onPageChange={(page) => update({ page })}
          onPageSizeChange={(pageSize) => update({ pageSize })}
        />
      )}

      <OrderFormDialog state={dialog} onClose={() => setDialog(null)} />
      <DeleteOrderDialog order={orderToDelete} onClose={() => setOrderToDelete(null)} />
    </div>
  )
}

function LoadingRows() {
  return (
    <div className="grid gap-2 p-4" aria-busy="true" aria-label="Carregando pedidos">
      {Array.from({ length: 8 }, (_, i) => (
        <Skeleton key={i} className="h-9" />
      ))}
    </div>
  )
}

function EmptyState({
  icon,
  title,
  description,
  action,
}: {
  icon: React.ReactNode
  title: string
  description?: string
  action?: React.ReactNode
}) {
  return (
    <div className="flex flex-col items-center gap-3 px-4 py-16 text-center">
      {icon}
      <p className="font-medium">{title}</p>
      {description && <p className="text-sm text-muted-foreground">{description}</p>}
      {action}
    </div>
  )
}

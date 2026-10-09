import { EyeIcon, PencilIcon, Trash2Icon } from 'lucide-react'
import type { Order, SortDirection, SortField } from '../model/types'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { formatCurrency, formatDateTime, formatOrderNumber } from '@/lib/format'
import { canDelete, isFinal } from '../model/orderStatus'
import { CustomerAvatar } from './CustomerAvatar'
import { SortableHead } from './SortableHead'
import { StatusBadge } from './StatusBadge'

interface OrdersTableProps {
  orders: Order[]
  sortBy: SortField
  sortDir: SortDirection
  onSort: (field: SortField) => void
  onView: (order: Order) => void
  onEdit: (order: Order) => void
  onDelete: (order: Order) => void
}

export function OrdersTable({ orders, sortBy, sortDir, onSort, onView, onEdit, onDelete }: OrdersTableProps) {
  const sortProps = { sortBy, sortDir, onSort }

  return (
    <Table>
      <TableHeader className="bg-muted/50 [&_th]:h-10 [&_th]:text-xs [&_th]:font-medium [&_th]:text-muted-foreground">
        <TableRow className="hover:bg-transparent">
          <SortableHead field="number" label="Nº" className="w-24 pl-4" {...sortProps} />
          <SortableHead field="customerName" label="Cliente" {...sortProps} />
          <TableHead>Descrição</TableHead>
          <SortableHead field="totalAmount" label="Valor" className="text-right" {...sortProps} />
          <SortableHead field="status" label="Status" {...sortProps} />
          <SortableHead field="createdAt" label="Criado em" {...sortProps} />
          <TableHead className="w-24 pr-4 text-right">Ações</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {orders.map((order) => (
          <TableRow
            key={order.id}
            tabIndex={0}
            aria-label={`Ver detalhes do pedido ${formatOrderNumber(order.number)}`}
            className="cursor-pointer focus-visible:bg-muted/50 focus-visible:outline-none"
            onClick={() => onView(order)}
            onKeyDown={(event) => {
              if (event.target === event.currentTarget && (event.key === 'Enter' || event.key === ' ')) {
                event.preventDefault()
                onView(order)
              }
            }}
          >
            <TableCell className="pl-4 font-mono text-xs font-medium text-muted-foreground">
              {formatOrderNumber(order.number)}
            </TableCell>
            <TableCell className="max-w-56" title={order.customerName}>
              <div className="flex items-center gap-2.5">
                <CustomerAvatar name={order.customerName} />
                <span className="truncate font-medium">{order.customerName}</span>
              </div>
            </TableCell>
            <TableCell className="max-w-72 truncate text-muted-foreground" title={order.description}>
              {order.description}
            </TableCell>
            <TableCell className="text-right font-medium tabular-nums">{formatCurrency(order.totalAmount)}</TableCell>
            <TableCell>
              <StatusBadge status={order.status} />
            </TableCell>
            <TableCell className="text-muted-foreground tabular-nums" title={`Atualizado em ${formatDateTime(order.updatedAt)}`}>
              {formatDateTime(order.createdAt)}
            </TableCell>
            <TableCell className="pr-4 text-right">
              <div className="flex justify-end gap-1" onClick={(event) => event.stopPropagation()}>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  aria-label={`${isFinal(order.status) ? 'Ver' : 'Editar'} pedido ${formatOrderNumber(order.number)}`}
                  onClick={() => onEdit(order)}
                >
                  {isFinal(order.status) ? <EyeIcon /> : <PencilIcon />}
                </Button>
                {canDelete(order.status) ? (
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    className="text-destructive hover:text-destructive"
                    aria-label={`Excluir pedido ${formatOrderNumber(order.number)}`}
                    onClick={() => onDelete(order)}
                  >
                    <Trash2Icon />
                  </Button>
                ) : (
                  <Tooltip>
                    <TooltipTrigger render={<span tabIndex={0} className="inline-flex" />}>
                      <Button variant="ghost" size="icon-sm" disabled aria-label="Exclusão indisponível">
                        <Trash2Icon />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>Pedidos pagos não podem ser excluídos.</TooltipContent>
                  </Tooltip>
                )}
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

import { EyeIcon, PencilIcon, Trash2Icon } from 'lucide-react'
import type { Order, SortDirection, SortField } from '@/api/types'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { formatCurrency, formatDateTime, formatOrderNumber } from '@/lib/format'
import { canDelete, isFinal } from './orderStatus'
import { SortableHead } from './SortableHead'
import { StatusBadge } from './StatusBadge'

interface OrdersTableProps {
  orders: Order[]
  sortBy: SortField
  sortDir: SortDirection
  onSort: (field: SortField) => void
  onEdit: (order: Order) => void
  onDelete: (order: Order) => void
}

export function OrdersTable({ orders, sortBy, sortDir, onSort, onEdit, onDelete }: OrdersTableProps) {
  const sortProps = { sortBy, sortDir, onSort }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <SortableHead field="number" label="Nº" className="w-24" {...sortProps} />
          <SortableHead field="customerName" label="Cliente" {...sortProps} />
          <TableHead>Descrição</TableHead>
          <SortableHead field="totalAmount" label="Valor" className="text-right" {...sortProps} />
          <SortableHead field="status" label="Status" {...sortProps} />
          <SortableHead field="createdAt" label="Criado em" {...sortProps} />
          <TableHead className="w-24 text-right">Ações</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {orders.map((order) => (
          <TableRow key={order.id}>
            <TableCell className="font-mono text-muted-foreground">{formatOrderNumber(order.number)}</TableCell>
            <TableCell className="max-w-56 truncate font-medium" title={order.customerName}>
              {order.customerName}
            </TableCell>
            <TableCell className="max-w-72 truncate text-muted-foreground" title={order.description}>
              {order.description}
            </TableCell>
            <TableCell className="text-right tabular-nums">{formatCurrency(order.totalAmount)}</TableCell>
            <TableCell>
              <StatusBadge status={order.status} />
            </TableCell>
            <TableCell className="tabular-nums" title={`Atualizado em ${formatDateTime(order.updatedAt)}`}>
              {formatDateTime(order.createdAt)}
            </TableCell>
            <TableCell className="text-right">
              <div className="flex justify-end gap-1">
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

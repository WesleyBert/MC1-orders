import type { ReactNode } from 'react'
import { CalendarIcon, PencilIcon, RefreshCwIcon, Trash2Icon } from 'lucide-react'
import type { Order } from '../model/types'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { formatCurrency, formatDateTime, formatOrderNumber } from '@/lib/format'
import { canDelete, immutableMessage, isFinal } from '../model/orderStatus'
import { CustomerAvatar } from './CustomerAvatar'
import { StatusBadge } from './StatusBadge'

interface OrderDetailsDialogProps {
  order: Order | null
  onClose: () => void
  onEdit: (order: Order) => void
  onDelete: (order: Order) => void
}

export function OrderDetailsDialog({ order, onClose, onEdit, onDelete }: OrderDetailsDialogProps) {
  return (
    <Dialog open={order !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">{order && <OrderDetails order={order} onEdit={onEdit} onDelete={onDelete} />}</DialogContent>
    </Dialog>
  )
}

function OrderDetails({ order, onEdit, onDelete }: Omit<OrderDetailsDialogProps, 'order' | 'onClose'> & { order: Order }) {
  const final = isFinal(order.status)

  return (
    <>
      <DialogHeader>
        <div className="flex items-center gap-2">
          <DialogTitle>Pedido {formatOrderNumber(order.number)}</DialogTitle>
          <StatusBadge status={order.status} />
        </div>
        <DialogDescription>{final ? immutableMessage(order.status) : 'Detalhes do pedido.'}</DialogDescription>
      </DialogHeader>

      <div className="flex items-center justify-between gap-4 rounded-xl border bg-muted/40 p-4">
        <div className="flex min-w-0 items-center gap-3">
          <CustomerAvatar name={order.customerName} className="size-10 text-sm" />
          <div className="min-w-0">
            <p className="text-xs text-muted-foreground">Cliente</p>
            <p className="truncate font-medium">{order.customerName}</p>
          </div>
        </div>
        <div className="text-right">
          <p className="text-xs text-muted-foreground">Valor total</p>
          <p className="text-lg font-semibold tabular-nums">{formatCurrency(order.totalAmount)}</p>
        </div>
      </div>

      <dl className="grid gap-4 text-sm">
        <Detail label="Descrição">
          <p className="break-words whitespace-pre-wrap">{order.description}</p>
        </Detail>
        <div className="grid gap-4 sm:grid-cols-2">
          <Detail label="Criado em" icon={<CalendarIcon />}>
            {formatDateTime(order.createdAt)}
          </Detail>
          <Detail label="Atualizado em" icon={<RefreshCwIcon />}>
            {formatDateTime(order.updatedAt)}
          </Detail>
        </div>
      </dl>

      {(!final || canDelete(order.status)) && (
        <DialogFooter>
          {canDelete(order.status) && (
            <Button variant="outline" className="text-destructive hover:text-destructive" onClick={() => onDelete(order)}>
              <Trash2Icon data-icon="inline-start" />
              Excluir
            </Button>
          )}
          {!final && (
            <Button onClick={() => onEdit(order)}>
              <PencilIcon data-icon="inline-start" />
              Editar
            </Button>
          )}
        </DialogFooter>
      )}
    </>
  )
}

function Detail({ label, icon, children }: { label: string; icon?: ReactNode; children: ReactNode }) {
  return (
    <div className="grid gap-1">
      <dt className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground [&_svg]:size-3.5">
        {icon}
        {label}
      </dt>
      <dd className="tabular-nums">{children}</dd>
    </div>
  )
}

import { toast } from 'sonner'
import type { Order } from '@/api/types'
import { ApiError } from '@/api/http'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { toastError } from '@/lib/errors'
import { formatOrderNumber } from '@/lib/format'
import { useDeleteOrder } from './queries'

interface DeleteOrderDialogProps {
  order: Order | null
  onClose: () => void
}

export function DeleteOrderDialog({ order, onClose }: DeleteOrderDialogProps) {
  const deleteOrder = useDeleteOrder()

  const confirm = () => {
    if (!order) {
      return
    }
    const number = formatOrderNumber(order.number)
    deleteOrder.mutate(order, {
      onSuccess: () => toast.success(`Pedido ${number} excluído.`),
      onError: (error) => {
        if (error instanceof ApiError && error.isNotFound) {
          toast.info('Este pedido já havia sido excluído.')
        } else if (error instanceof ApiError && error.isPreconditionFailed) {
          toast.error('Este pedido foi alterado por outra pessoa. A lista foi atualizada.')
        } else {
          toastError(error)
        }
      },
      onSettled: onClose,
    })
  }

  return (
    <AlertDialog open={order !== null} onOpenChange={(open) => !open && !deleteOrder.isPending && onClose()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Excluir pedido {order && formatOrderNumber(order.number)}?</AlertDialogTitle>
          <AlertDialogDescription>
            {order && `Excluir o pedido ${formatOrderNumber(order.number)} de ${order.customerName}? `}
            Esta ação não pode ser desfeita.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={deleteOrder.isPending}>Cancelar</AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={confirm} disabled={deleteOrder.isPending}>
            {deleteOrder.isPending ? 'Excluindo…' : 'Excluir'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

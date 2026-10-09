import { useState } from 'react'
import { toast } from 'sonner'
import { RotateCwIcon, TriangleAlertIcon } from 'lucide-react'
import { ApiError } from '@/lib/http/client'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { errorMessage, toastError } from '@/lib/errors'
import { formatDateTime, formatOrderNumber, parseAmount } from '@/lib/format'
import { ORDER_FORM_ID, OrderForm } from './OrderForm'
import type { OrderFormValues } from '../model/orderFormSchema'
import { immutableMessage, isFinal, STATUS_LABELS } from '../model/orderStatus'
import { useCreateOrder, useOrder, useUpdateOrder } from '../api/queries'

export type OrderDialogState = { mode: 'create' } | { mode: 'edit'; orderId: string }

interface OrderFormDialogProps {
  state: OrderDialogState | null
  onClose: () => void
}

export function OrderFormDialog({ state, onClose }: OrderFormDialogProps) {
  return (
    <Dialog open={state !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        {state?.mode === 'create' && <CreateOrderContent onClose={onClose} />}
        {state?.mode === 'edit' && <EditOrderContent key={state.orderId} orderId={state.orderId} onClose={onClose} />}
      </DialogContent>
    </Dialog>
  )
}

function toInput(values: OrderFormValues) {
  return {
    customerName: values.customerName.trim(),
    description: values.description.trim(),
    totalAmount: parseAmount(values.totalAmount) ?? 0,
  }
}

function CreateOrderContent({ onClose }: { onClose: () => void }) {
  const createOrder = useCreateOrder()

  const submit = (values: OrderFormValues) =>
    createOrder.mutate(toInput(values), {
      onSuccess: (order) => {
        toast.success(`Pedido ${formatOrderNumber(order.number)} criado com sucesso.`)
        onClose()
      },
      onError: (error) => {
        if (!(error instanceof ApiError && error.isValidation)) {
          toastError(error)
        }
      },
    })

  return (
    <>
      <DialogHeader>
        <DialogTitle>Novo pedido</DialogTitle>
        <DialogDescription>O pedido é criado com status Aberto.</DialogDescription>
      </DialogHeader>
      <OrderForm order={undefined} readOnly={false} serverError={createOrder.error} onSubmit={submit} />
      <FormFooter saving={createOrder.isPending} onCancel={onClose} />
    </>
  )
}

function EditOrderContent({ orderId, onClose }: { orderId: string; onClose: () => void }) {
  const orderQuery = useOrder(orderId)
  const updateOrder = useUpdateOrder()
  const [conflict, setConflict] = useState(false)
  const order = orderQuery.data

  if (orderQuery.isPending) {
    return <EditLoading />
  }

  if (orderQuery.isError || !order) {
    return (
      <>
        <DialogHeader>
          <DialogTitle>Pedido indisponível</DialogTitle>
        </DialogHeader>
        <Alert variant="destructive">
          <TriangleAlertIcon />
          <AlertDescription>
            {orderQuery.error instanceof ApiError && orderQuery.error.isNotFound
              ? 'Este pedido não existe mais.'
              : errorMessage(orderQuery.error)}
          </AlertDescription>
        </Alert>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Fechar
          </Button>
        </DialogFooter>
      </>
    )
  }

  const readOnly = isFinal(order.status)

  const reload = async () => {
    setConflict(false)
    updateOrder.reset()
    await orderQuery.refetch()
  }

  const submit = (values: OrderFormValues) =>
    updateOrder.mutate(
      { order, input: { ...toInput(values), status: values.status } },
      {
        onSuccess: (saved) => {
          const message =
            saved.status !== order.status
              ? `Pedido ${formatOrderNumber(saved.number)} marcado como ${STATUS_LABELS[saved.status]}.`
              : `Pedido ${formatOrderNumber(saved.number)} atualizado.`
          toast.success(message)
          onClose()
        },
        onError: (error) => {
          if (error instanceof ApiError && error.isPreconditionFailed) {
            setConflict(true)
          } else if (error instanceof ApiError && error.isNotFound) {
            toast.error('Este pedido não existe mais.')
            onClose()
          } else if (!(error instanceof ApiError && (error.isValidation || error.isConflict))) {
            toastError(error)
          }
        },
      },
    )

  const businessError =
    updateOrder.error instanceof ApiError && updateOrder.error.isConflict ? updateOrder.error.message : null

  return (
    <>
      <DialogHeader>
        <DialogTitle>
          {readOnly ? 'Pedido' : 'Editar pedido'} {formatOrderNumber(order.number)}
        </DialogTitle>
        <DialogDescription>
          Criado em {formatDateTime(order.createdAt)} · Atualizado em {formatDateTime(order.updatedAt)}
        </DialogDescription>
      </DialogHeader>

      {readOnly && (
        <Alert>
          <TriangleAlertIcon />
          <AlertDescription>{immutableMessage(order.status)}</AlertDescription>
        </Alert>
      )}

      {conflict && (
        <Alert variant="destructive">
          <TriangleAlertIcon />
          <AlertTitle>Este pedido foi alterado por outra pessoa.</AlertTitle>
          <AlertDescription>
            <p>Seus dados continuam no formulário. Recarregue para ver a versão atual antes de salvar.</p>
            <Button variant="outline" size="sm" className="mt-2" onClick={reload} disabled={orderQuery.isFetching}>
              <RotateCwIcon data-icon="inline-start" />
              Recarregar dados
            </Button>
          </AlertDescription>
        </Alert>
      )}

      {businessError && (
        <Alert variant="destructive">
          <TriangleAlertIcon />
          <AlertDescription>{businessError}</AlertDescription>
        </Alert>
      )}

      <OrderForm
        key={order.version}
        order={order}
        readOnly={readOnly}
        serverError={updateOrder.error}
        onSubmit={submit}
      />

      {readOnly ? (
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Fechar
          </Button>
        </DialogFooter>
      ) : (
        <FormFooter saving={updateOrder.isPending} disabled={conflict} onCancel={onClose} />
      )}
    </>
  )
}

function FormFooter({ saving, disabled = false, onCancel }: { saving: boolean; disabled?: boolean; onCancel: () => void }) {
  return (
    <DialogFooter>
      <Button variant="outline" onClick={onCancel} disabled={saving}>
        Cancelar
      </Button>
      <Button type="submit" form={ORDER_FORM_ID} disabled={saving || disabled}>
        {saving ? 'Salvando…' : 'Salvar'}
      </Button>
    </DialogFooter>
  )
}

function EditLoading() {
  return (
    <>
      <DialogHeader>
        <DialogTitle>Carregando pedido…</DialogTitle>
      </DialogHeader>
      <div className="grid gap-3" aria-busy="true">
        <Skeleton className="h-9" />
        <Skeleton className="h-20" />
        <Skeleton className="h-9" />
      </div>
    </>
  )
}

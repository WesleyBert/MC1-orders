import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import type { Order, OrderStatus } from '../model/types'
import { ApiError } from '@/lib/http/client'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { formatAmountInput, maskAmountInput } from '@/lib/format'
import { isOrderFormField, orderFormSchema, type OrderFormValues } from '../model/orderFormSchema'
import { allowedTargets, STATUS_LABELS } from '../model/orderStatus'

export const ORDER_FORM_ID = 'order-form'

interface OrderFormProps {
  order: Order | undefined
  readOnly: boolean
  serverError: unknown
  onSubmit: (values: OrderFormValues) => void
}

const emptyValues: OrderFormValues = { customerName: '', description: '', totalAmount: '', status: 'Open' }

function toFormValues(order: Order | undefined): OrderFormValues {
  return order
    ? {
        customerName: order.customerName,
        description: order.description,
        totalAmount: formatAmountInput(order.totalAmount),
        status: order.status,
      }
    : emptyValues
}

export function OrderForm({ order, readOnly, serverError, onSubmit }: OrderFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<OrderFormValues>({
    resolver: zodResolver(orderFormSchema),
    defaultValues: toFormValues(order),
  })

  useEffect(() => {
    if (!(serverError instanceof ApiError) || !serverError.isValidation) {
      return
    }
    for (const [field, messages] of Object.entries(serverError.fieldErrors)) {
      if (isOrderFormField(field) && messages[0]) {
        setError(field, { type: 'server', message: messages[0] }, { shouldFocus: true })
      }
    }
  }, [serverError, setError])

  const statusOptions: readonly OrderStatus[] = order ? allowedTargets(order.status) : []
  const statusItems = Object.fromEntries(statusOptions.map((s) => [s, STATUS_LABELS[s]]))

  return (
    <form id={ORDER_FORM_ID} noValidate onSubmit={handleSubmit(onSubmit)} className="grid gap-4">
      <Field id="customerName" label="Cliente" error={errors.customerName?.message}>
        <Input
          id="customerName"
          autoComplete="off"
          maxLength={150}
          disabled={readOnly}
          aria-invalid={errors.customerName ? true : undefined}
          aria-describedby={errors.customerName ? 'customerName-error' : undefined}
          {...register('customerName')}
        />
      </Field>

      <Field id="description" label="Descrição" error={errors.description?.message}>
        <Textarea
          id="description"
          rows={3}
          maxLength={500}
          disabled={readOnly}
          aria-invalid={errors.description ? true : undefined}
          aria-describedby={errors.description ? 'description-error' : undefined}
          {...register('description')}
        />
      </Field>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field id="totalAmount" label="Valor total" error={errors.totalAmount?.message}>
          <Controller
            control={control}
            name="totalAmount"
            render={({ field }) => (
              <div className="relative">
                <span
                  aria-hidden="true"
                  className="pointer-events-none absolute top-1/2 left-2.5 -translate-y-1/2 text-sm text-muted-foreground"
                >
                  R$
                </span>
                <Input
                  id="totalAmount"
                  inputMode="numeric"
                  placeholder="0,00"
                  autoComplete="off"
                  className="pl-9 text-right tabular-nums"
                  disabled={readOnly}
                  aria-invalid={errors.totalAmount ? true : undefined}
                  aria-describedby={errors.totalAmount ? 'totalAmount-error' : undefined}
                  name={field.name}
                  ref={field.ref}
                  value={field.value}
                  onBlur={field.onBlur}
                  onChange={(event) => field.onChange(maskAmountInput(event.target.value))}
                />
              </div>
            )}
          />
        </Field>

        {order && !readOnly && (
          <Field id="status" label="Status" error={errors.status?.message}>
            <Controller
              control={control}
              name="status"
              render={({ field }) => (
                <Select
                  items={statusItems}
                  value={field.value}
                  onValueChange={(value) => value && field.onChange(value)}
                >
                  <SelectTrigger id="status" className="h-9 w-full" onBlur={field.onBlur}>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {statusOptions.map((s) => (
                      <SelectItem key={s} value={s}>
                        {STATUS_LABELS[s]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </Field>
        )}
      </div>
    </form>
  )
}

function Field({
  id,
  label,
  error,
  children,
}: {
  id: string
  label: string
  error: string | undefined
  children: React.ReactNode
}) {
  return (
    <div className="grid gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      {children}
      {error && (
        <p id={`${id}-error`} className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  )
}

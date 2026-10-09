import type { OrderStatus } from '../model/types'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { STATUS_LABELS, STATUS_STYLES } from '../model/orderStatus'

export function StatusBadge({ status }: { status: OrderStatus }) {
  const styles = STATUS_STYLES[status]
  return (
    <Badge className={cn('gap-1.5 border-transparent font-medium ring-1 ring-inset', styles.badge)}>
      <span className={cn('size-1.5 rounded-full', styles.dot)} aria-hidden="true" />
      {STATUS_LABELS[status]}
    </Badge>
  )
}

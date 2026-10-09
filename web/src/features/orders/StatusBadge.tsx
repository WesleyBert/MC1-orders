import type { OrderStatus } from '@/api/types'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { STATUS_LABELS, STATUS_STYLES } from './orderStatus'

export function StatusBadge({ status }: { status: OrderStatus }) {
  return <Badge className={cn('border-transparent font-medium', STATUS_STYLES[status])}>{STATUS_LABELS[status]}</Badge>
}

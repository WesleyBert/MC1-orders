import type { LucideIcon } from 'lucide-react'
import { CircleCheckIcon, CircleDotIcon, CircleSlashIcon, LayersIcon } from 'lucide-react'
import type { OrderStatus } from '../model/types'
import { Skeleton } from '@/components/ui/skeleton'
import { formatInteger } from '@/lib/format'
import { cn } from '@/lib/utils'
import { useOrdersSummary } from '../api/queries'

interface OrdersSummaryProps {
  status: OrderStatus | undefined
  onStatusChange: (status: OrderStatus | undefined) => void
}

interface SummaryCard {
  label: string
  status: OrderStatus | undefined
  count: number | undefined
  icon: LucideIcon
  iconClassName: string
}

export function OrdersSummary({ status, onStatusChange }: OrdersSummaryProps) {
  const summary = useOrdersSummary()

  const cards: SummaryCard[] = [
    { label: 'Total de pedidos', status: undefined, count: summary.total, icon: LayersIcon, iconClassName: 'bg-primary/10 text-primary' },
    { label: 'Abertos', status: 'Open', count: summary.open, icon: CircleDotIcon, iconClassName: 'bg-blue-500/10 text-blue-600' },
    { label: 'Pagos', status: 'Paid', count: summary.paid, icon: CircleCheckIcon, iconClassName: 'bg-emerald-500/10 text-emerald-600' },
    { label: 'Cancelados', status: 'Cancelled', count: summary.cancelled, icon: CircleSlashIcon, iconClassName: 'bg-zinc-500/10 text-zinc-600' },
  ]

  return (
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4" role="group" aria-label="Resumo por status">
      {cards.map((card) => {
        const active = card.status === status
        return (
          <button
            key={card.label}
            type="button"
            aria-pressed={active}
            onClick={() => onStatusChange(card.status)}
            className={cn(
              'flex items-center gap-3 rounded-xl border bg-card p-4 text-left shadow-xs transition-all hover:border-primary/40 hover:shadow-sm focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none',
              active && 'border-primary ring-1 ring-primary',
            )}
          >
            <div className={cn('flex size-10 shrink-0 items-center justify-center rounded-lg', card.iconClassName)}>
              <card.icon className="size-5" />
            </div>
            <div className="min-w-0">
              <p className="truncate text-xs font-medium text-muted-foreground">{card.label}</p>
              {card.count !== undefined ? (
                <p className="text-xl font-semibold tabular-nums">{formatInteger(card.count)}</p>
              ) : summary.failed ? (
                <p className="text-xl font-semibold text-muted-foreground" title="Não foi possível carregar o resumo">
                  —
                </p>
              ) : (
                <Skeleton className="mt-1 h-6 w-16" />
              )}
            </div>
          </button>
        )
      })}
    </div>
  )
}

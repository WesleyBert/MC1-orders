import { ArrowDownIcon, ArrowUpDownIcon, ArrowUpIcon } from 'lucide-react'
import type { SortDirection, SortField } from '@/api/types'
import { TableHead } from '@/components/ui/table'
import { cn } from '@/lib/utils'

interface SortableHeadProps {
  field: SortField
  label: string
  sortBy: SortField
  sortDir: SortDirection
  onSort: (field: SortField) => void
  className?: string
}

export function SortableHead({ field, label, sortBy, sortDir, onSort, className }: SortableHeadProps) {
  const active = field === sortBy
  const Icon = !active ? ArrowUpDownIcon : sortDir === 'asc' ? ArrowUpIcon : ArrowDownIcon

  return (
    <TableHead
      className={className}
      aria-sort={active ? (sortDir === 'asc' ? 'ascending' : 'descending') : 'none'}
    >
      <button
        type="button"
        onClick={() => onSort(field)}
        className={cn(
          'inline-flex items-center gap-1 rounded-sm hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
          active && 'text-foreground',
        )}
      >
        {label}
        <Icon className={cn('size-3.5', !active && 'opacity-40')} />
      </button>
    </TableHead>
  )
}

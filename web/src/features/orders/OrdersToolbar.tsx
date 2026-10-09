import { useEffect, useState } from 'react'
import { PlusIcon, SearchIcon } from 'lucide-react'
import type { OrderStatus } from '@/api/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { ORDER_STATUSES, STATUS_LABELS } from './orderStatus'

const ALL = 'all'

const STATUS_FILTER_ITEMS: Record<string, string> = { [ALL]: 'Todos os status', ...STATUS_LABELS }

interface OrdersToolbarProps {
  search: string
  status: OrderStatus | undefined
  onSearchChange: (search: string) => void
  onStatusChange: (status: OrderStatus | undefined) => void
  onCreate: () => void
}

export function OrdersToolbar({ search, status, onSearchChange, onStatusChange, onCreate }: OrdersToolbarProps) {
  const [term, setTerm] = useState(search)
  const [syncedSearch, setSyncedSearch] = useState(search)
  const debouncedTerm = useDebouncedValue(term, 300)

  if (search !== syncedSearch) {
    setSyncedSearch(search)
    if (search !== term.trim()) {
      setTerm(search)
    }
  }

  useEffect(() => {
    if (debouncedTerm === term && debouncedTerm.trim() !== search) {
      onSearchChange(debouncedTerm.trim())
    }
  }, [debouncedTerm, term, search, onSearchChange])

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
      <div className="relative flex-1">
        <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          aria-label="Buscar pedidos"
          placeholder="Buscar por número, cliente ou descrição"
          className="h-9 pl-8"
          maxLength={100}
          value={term}
          onChange={(event) => setTerm(event.target.value)}
        />
      </div>

      <Select
        items={STATUS_FILTER_ITEMS}
        value={status ?? ALL}
        onValueChange={(value) => onStatusChange(ORDER_STATUSES.find((s) => s === value))}
      >
        <SelectTrigger aria-label="Filtrar por status" className="h-9 w-full sm:w-44">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={ALL}>Todos os status</SelectItem>
          {ORDER_STATUSES.map((s) => (
            <SelectItem key={s} value={s}>
              {STATUS_LABELS[s]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Button size="lg" onClick={onCreate}>
        <PlusIcon data-icon="inline-start" />
        Novo pedido
      </Button>
    </div>
  )
}

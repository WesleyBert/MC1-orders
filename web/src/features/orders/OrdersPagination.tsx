import { ChevronLeftIcon, ChevronRightIcon, ChevronsLeftIcon, ChevronsRightIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { formatInteger } from '@/lib/format'
import { PAGE_SIZES } from './useOrdersUrlState'

interface OrdersPaginationProps {
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
}

export function OrdersPagination({
  page,
  pageSize,
  totalItems,
  totalPages,
  onPageChange,
  onPageSizeChange,
}: OrdersPaginationProps) {
  const first = totalItems === 0 ? 0 : (page - 1) * pageSize + 1
  const last = Math.min(page * pageSize, totalItems)
  const isFirst = page <= 1
  const isLast = page >= totalPages

  return (
    <div className="flex flex-col items-center justify-between gap-3 text-sm text-muted-foreground sm:flex-row">
      <p aria-live="polite">
        Exibindo {formatInteger(first)}–{formatInteger(last)} de {formatInteger(totalItems)} pedidos
      </p>

      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2">
          <span>Por página</span>
          <Select
            items={Object.fromEntries(PAGE_SIZES.map((size) => [String(size), String(size)]))}
            value={String(pageSize)}
            onValueChange={(value) => value && onPageSizeChange(Number(value))}
          >
            <SelectTrigger size="sm" aria-label="Itens por página" className="w-18">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {PAGE_SIZES.map((size) => (
                <SelectItem key={size} value={String(size)}>
                  {size}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <span className="tabular-nums">
          Página {formatInteger(totalPages === 0 ? 0 : page)} de {formatInteger(totalPages)}
        </span>

        <nav className="flex gap-1" aria-label="Paginação">
          <Button variant="outline" size="icon-sm" aria-label="Primeira página" disabled={isFirst} onClick={() => onPageChange(1)}>
            <ChevronsLeftIcon />
          </Button>
          <Button variant="outline" size="icon-sm" aria-label="Página anterior" disabled={isFirst} onClick={() => onPageChange(page - 1)}>
            <ChevronLeftIcon />
          </Button>
          <Button variant="outline" size="icon-sm" aria-label="Próxima página" disabled={isLast} onClick={() => onPageChange(page + 1)}>
            <ChevronRightIcon />
          </Button>
          <Button variant="outline" size="icon-sm" aria-label="Última página" disabled={isLast} onClick={() => onPageChange(totalPages)}>
            <ChevronsRightIcon />
          </Button>
        </nav>
      </div>
    </div>
  )
}

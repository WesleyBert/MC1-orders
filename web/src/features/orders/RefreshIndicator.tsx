import { RefreshCwIcon, TriangleAlertIcon } from 'lucide-react'
import { formatTime } from '@/lib/format'
import { cn } from '@/lib/utils'

interface RefreshIndicatorProps {
  updatedAt: number
  isFetching: boolean
  failed: boolean
}

export function RefreshIndicator({ updatedAt, isFetching, failed }: RefreshIndicatorProps) {
  if (failed) {
    return (
      <p className="flex items-center gap-1.5 text-xs text-amber-700" role="status">
        <TriangleAlertIcon className="size-3.5" />
        Falha ao atualizar. Nova tentativa em 10 s.
      </p>
    )
  }

  return (
    <p className="flex items-center gap-1.5 text-xs text-muted-foreground" role="status">
      <RefreshCwIcon className={cn('size-3.5', isFetching && 'animate-spin')} />
      {updatedAt > 0 ? `Atualizado às ${formatTime(updatedAt)}` : 'Carregando…'}
      <span className="sr-only">. A lista é atualizada automaticamente a cada 10 segundos.</span>
    </p>
  )
}

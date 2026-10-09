import { cn } from '@/lib/utils'

function initials(name: string) {
  const words = name.split(/[\s-]+/).filter(Boolean)
  return ((words[0]?.[0] ?? '') + (words.length > 1 ? (words.at(-1)?.[0] ?? '') : '')).toUpperCase()
}

export function CustomerAvatar({ name, className }: { name: string; className?: string }) {
  return (
    <span
      className={cn(
        'flex size-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary',
        className,
      )}
      aria-hidden="true"
    >
      {initials(name)}
    </span>
  )
}

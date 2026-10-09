import type { ReactNode } from 'react'
import { PackageIcon } from 'lucide-react'

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-svh bg-muted/40">
      <header className="sticky top-0 z-10 border-b bg-background/80 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-7xl items-center gap-3 px-4 sm:px-6">
          <div className="flex size-8 items-center justify-center rounded-lg bg-primary text-primary-foreground shadow-sm">
            <PackageIcon className="size-4" />
          </div>
          <div className="flex items-baseline gap-2">
            <span className="font-semibold tracking-tight">MC1 Orders</span>
            <span className="hidden text-xs text-muted-foreground sm:inline">Backoffice</span>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">{children}</main>
    </div>
  )
}

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiError } from '@/api/http'
import { Toaster } from '@/components/ui/sonner'
import { TooltipProvider } from '@/components/ui/tooltip'
import { OrdersPage } from '@/features/orders/OrdersPage'

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: (failureCount, error) => !(error instanceof ApiError && error.status < 500) && failureCount < 1,
        refetchOnWindowFocus: true,
      },
      mutations: {
        retry: false,
      },
    },
  })
}

export function App() {
  const [queryClient] = useState(createQueryClient)

  return (
    <QueryClientProvider client={queryClient}>
      <TooltipProvider>
        <OrdersPage />
        <Toaster position="top-right" richColors closeButton />
      </TooltipProvider>
    </QueryClientProvider>
  )
}

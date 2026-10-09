import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/lib/http/client'

export function createQueryClient() {
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

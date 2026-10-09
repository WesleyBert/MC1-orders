import { toast } from 'sonner'
import { ApiError, NetworkError } from './http/client'

export function errorMessage(error: unknown) {
  if (error instanceof ApiError || error instanceof NetworkError) {
    return error.message
  }
  return 'Ocorreu um erro inesperado.'
}

export function toastError(error: unknown) {
  const traceId = error instanceof ApiError ? error.traceId : undefined
  toast.error(errorMessage(error), {
    description: traceId ? `Código de rastreio: ${traceId}` : undefined,
  })
}

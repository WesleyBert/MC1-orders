import type { ProblemDetails } from './types'

const REQUEST_TIMEOUT_MS = 10_000

export class ApiError extends Error {
  readonly status: number
  readonly code: string | undefined
  readonly traceId: string | undefined
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? 'Ocorreu um erro inesperado.')
    this.name = 'ApiError'
    this.status = status
    this.code = problem.code
    this.traceId = problem.traceId
    this.fieldErrors = problem.errors ?? {}
  }

  get isNotFound() {
    return this.status === 404
  }

  get isPreconditionFailed() {
    return this.status === 412
  }

  get isConflict() {
    return this.status === 409
  }

  get isValidation() {
    return this.status === 400 && Object.keys(this.fieldErrors).length > 0
  }
}

export class NetworkError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'NetworkError'
  }
}

export interface ApiResponse<T> {
  data: T
  etag: string | null
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  ifMatch?: string
  signal?: AbortSignal
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<ApiResponse<T>> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }
  if (options.ifMatch) {
    headers['If-Match'] = options.ifMatch
  }

  const timeout = AbortSignal.timeout(REQUEST_TIMEOUT_MS)
  const signal = options.signal ? AbortSignal.any([options.signal, timeout]) : timeout

  let response: Response
  try {
    response = await fetch(path, {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal,
    })
  } catch (error) {
    if (options.signal?.aborted) {
      throw error
    }
    throw new NetworkError(
      timeout.aborted
        ? 'O servidor demorou para responder. Tente novamente.'
        : 'Não foi possível conectar ao servidor.',
    )
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response))
  }

  const data = response.status === 204 ? (undefined as T) : ((await response.json()) as T)
  return { data, etag: response.headers.get('ETag') }
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return { status: response.status, title: response.statusText }
  }
}

export function toETag(version: number) {
  return `"${version}"`
}

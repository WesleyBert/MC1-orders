import { ApiError, NetworkError, request } from './http'

function mockFetch(response: Response) {
  return vi.spyOn(globalThis, 'fetch').mockResolvedValue(response)
}

afterEach(() => vi.restoreAllMocks())

describe('request', () => {
  it('devolve dados e ETag em caso de sucesso', async () => {
    mockFetch(new Response(JSON.stringify({ id: '1' }), { status: 200, headers: { ETag: '"3"' } }))

    const result = await request<{ id: string }>('/api/v1/orders/1')

    expect(result).toEqual({ data: { id: '1' }, etag: '"3"' })
  })

  it('envia If-Match e corpo JSON', async () => {
    const fetchSpy = mockFetch(new Response(JSON.stringify({}), { status: 200 }))

    await request('/api/v1/orders/1', { method: 'PUT', body: { a: 1 }, ifMatch: '"2"' })

    const [, init] = fetchSpy.mock.calls[0]!
    expect(init?.headers).toMatchObject({ 'If-Match': '"2"', 'Content-Type': 'application/json' })
    expect(init?.body).toBe('{"a":1}')
  })

  it('converte problem details em ApiError com erros por campo', async () => {
    mockFetch(
      new Response(
        JSON.stringify({
          status: 400,
          title: 'Um ou mais campos são inválidos.',
          code: 'request.validation',
          traceId: 'abc',
          errors: { customerName: ['O nome do cliente é obrigatório.'] },
        }),
        { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
      ),
    )

    const error = await request('/api/v1/orders').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 400,
      code: 'request.validation',
      traceId: 'abc',
      isValidation: true,
      fieldErrors: { customerName: ['O nome do cliente é obrigatório.'] },
    })
  })

  it('usa o detail como mensagem em conflitos', async () => {
    mockFetch(
      new Response(JSON.stringify({ status: 412, detail: 'Este pedido foi alterado por outra pessoa.' }), { status: 412 }),
    )

    const error = (await request('/x').catch((e: unknown) => e)) as ApiError

    expect(error.isPreconditionFailed).toBe(true)
    expect(error.message).toBe('Este pedido foi alterado por outra pessoa.')
  })

  it('trata respostas sem corpo JSON', async () => {
    mockFetch(new Response('Bad Gateway', { status: 502, statusText: 'Bad Gateway' }))

    const error = (await request('/x').catch((e: unknown) => e)) as ApiError

    expect(error.status).toBe(502)
    expect(error.message).toBe('Bad Gateway')
  })

  it('converte falha de rede em NetworkError', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'))

    await expect(request('/x')).rejects.toBeInstanceOf(NetworkError)
  })

  it('aceita 204 sem corpo', async () => {
    mockFetch(new Response(null, { status: 204 }))

    await expect(request('/x', { method: 'DELETE' })).resolves.toEqual({ data: undefined, etag: null })
  })
})

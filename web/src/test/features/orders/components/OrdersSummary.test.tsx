import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { OrdersSummary } from '@/features/orders/components/OrdersSummary'

function renderSummary(response: Response) {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(response)
  const onStatusChange = vi.fn()
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <OrdersSummary status={undefined} onStatusChange={onStatusChange} />
    </QueryClientProvider>,
  )
  return { onStatusChange }
}

afterEach(() => vi.restoreAllMocks())

describe('OrdersSummary', () => {
  it('mostra o total e a quantidade por status vindos de uma única chamada', async () => {
    renderSummary(new Response(JSON.stringify({ total: 10000, open: 6003, paid: 2990, cancelled: 1007 })))

    expect(await screen.findByText('10.000')).toBeInTheDocument()
    expect(screen.getByText('6.003')).toBeInTheDocument()
    expect(screen.getByText('2.990')).toBeInTheDocument()
    expect(screen.getByText('1.007')).toBeInTheDocument()
    expect(globalThis.fetch).toHaveBeenCalledTimes(1)
    expect(vi.mocked(globalThis.fetch).mock.calls[0]![0]).toBe('/api/v1/orders/summary')
  })

  it('mostra um traço em vez de carregar para sempre quando a API falha', async () => {
    renderSummary(new Response(JSON.stringify({ status: 500 }), { status: 500 }))

    expect(await screen.findAllByText('—')).toHaveLength(4)
  })

  it('filtra pelo status ao clicar no card', async () => {
    const { onStatusChange } = renderSummary(
      new Response(JSON.stringify({ total: 3, open: 1, paid: 1, cancelled: 1 })),
    )

    await userEvent.click(await screen.findByRole('button', { name: /Pagos/ }))

    expect(onStatusChange).toHaveBeenCalledWith('Paid')
  })
})

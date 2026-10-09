import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { TooltipProvider } from '@/components/ui/tooltip'
import type { Order } from '@/features/orders/model/types'
import { OrdersTable } from '@/features/orders/components/OrdersTable'

const order: Order = {
  id: '0199c3a2-7f1e-7b6a-9d2e-4f5a6b7c8d9e',
  number: 10234,
  customerName: 'Maria Souza',
  description: 'Reposição de gôndola',
  totalAmount: 1520.5,
  status: 'Open',
  createdAt: '2026-10-08T12:00:00Z',
  updatedAt: '2026-10-08T12:00:00Z',
  version: 1,
}

function renderTable() {
  const handlers = { onSort: vi.fn(), onView: vi.fn(), onEdit: vi.fn(), onDelete: vi.fn() }
  render(
    <TooltipProvider>
      <OrdersTable orders={[order]} sortBy="createdAt" sortDir="desc" {...handlers} />
    </TooltipProvider>,
  )
  return handlers
}

describe('OrdersTable', () => {
  it('abre os detalhes ao clicar na linha', async () => {
    const { onView } = renderTable()

    await userEvent.click(screen.getByText('Maria Souza'))

    expect(onView).toHaveBeenCalledWith(order)
  })

  it('abre os detalhes com Enter na linha focada', async () => {
    const { onView } = renderTable()

    screen.getByRole('row', { name: 'Ver detalhes do pedido #10234' }).focus()
    await userEvent.keyboard('{Enter}')

    expect(onView).toHaveBeenCalledWith(order)
  })

  it('os botões de ação não abrem os detalhes', async () => {
    const { onView, onEdit, onDelete } = renderTable()

    await userEvent.click(screen.getByRole('button', { name: 'Editar pedido #10234' }))
    await userEvent.click(screen.getByRole('button', { name: 'Excluir pedido #10234' }))

    expect(onEdit).toHaveBeenCalledWith(order)
    expect(onDelete).toHaveBeenCalledWith(order)
    expect(onView).not.toHaveBeenCalled()
  })
})

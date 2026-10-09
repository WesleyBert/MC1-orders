import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { Order } from '@/features/orders/model/types'
import { OrderDetailsDialog } from '@/features/orders/components/OrderDetailsDialog'

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

function renderDialog(value: Order) {
  const handlers = { onClose: vi.fn(), onEdit: vi.fn(), onDelete: vi.fn() }
  render(<OrderDetailsDialog order={value} {...handlers} />)
  return handlers
}

describe('OrderDetailsDialog', () => {
  it('mostra as informações do pedido', () => {
    renderDialog(order)

    expect(screen.getByRole('heading', { name: 'Pedido #10234' })).toBeInTheDocument()
    expect(screen.getByText('Maria Souza')).toBeInTheDocument()
    expect(screen.getByText('Reposição de gôndola')).toBeInTheDocument()
    expect(screen.getByText(/R\$\s1\.520,50/)).toBeInTheDocument()
    expect(screen.getByText('Aberto')).toBeInTheDocument()
  })

  it('permite editar e excluir um pedido aberto', async () => {
    const { onEdit, onDelete } = renderDialog(order)

    await userEvent.click(screen.getByRole('button', { name: 'Editar' }))
    await userEvent.click(screen.getByRole('button', { name: 'Excluir' }))

    expect(onEdit).toHaveBeenCalledWith(order)
    expect(onDelete).toHaveBeenCalledWith(order)
  })

  it('não oferece editar nem excluir um pedido pago', () => {
    renderDialog({ ...order, status: 'Paid' })

    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Excluir' })).not.toBeInTheDocument()
    expect(screen.getByText('Pedidos com status Pago não podem ser alterados.')).toBeInTheDocument()
  })
})

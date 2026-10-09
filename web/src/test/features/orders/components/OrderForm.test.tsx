import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ApiError } from '@/lib/http/client'
import type { Order } from '@/features/orders/model/types'
import { ORDER_FORM_ID, OrderForm } from '@/features/orders/components/OrderForm'

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

function renderForm(props: Partial<React.ComponentProps<typeof OrderForm>> = {}) {
  const onSubmit = vi.fn()
  render(
    <>
      <OrderForm order={undefined} readOnly={false} serverError={null} onSubmit={onSubmit} {...props} />
      <button type="submit" form={ORDER_FORM_ID}>
        Salvar
      </button>
    </>,
  )
  return { onSubmit }
}

describe('OrderForm', () => {
  it('limita o tamanho dos campos de texto e mostra o contador de caracteres', async () => {
    renderForm()
    const customerName = screen.getByLabelText('Cliente')
    const description = screen.getByLabelText('Descrição')

    expect(customerName).toHaveAttribute('maxLength', '15')
    expect(description).toHaveAttribute('maxLength', '500')
    expect(screen.getByText('0/15')).toBeInTheDocument()

    await userEvent.type(customerName, 'Maria')

    expect(screen.getByText('5/15')).toBeInTheDocument()
    expect(screen.getByText('0/500')).toBeInTheDocument()
  })

  it('não mostra o contador quando o pedido é somente leitura', () => {
    renderForm({ order: { ...order, status: 'Paid' }, readOnly: true })

    expect(screen.queryByText(/^\d+\/\d+$/)).not.toBeInTheDocument()
  })

  it('mostra os erros de validação e não envia', async () => {
    const { onSubmit } = renderForm()

    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

    expect(await screen.findByText('O nome do cliente é obrigatório.')).toBeInTheDocument()
    expect(screen.getByText('A descrição é obrigatória.')).toBeInTheDocument()
    expect(screen.getByText('O valor total é obrigatório.')).toBeInTheDocument()
    expect(screen.getByLabelText('Cliente')).toHaveAttribute('aria-invalid', 'true')
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('envia os valores digitados', async () => {
    const { onSubmit } = renderForm()

    await userEvent.type(screen.getByLabelText('Cliente'), 'Maria Souza')
    await userEvent.type(screen.getByLabelText('Descrição'), 'Pedido mensal')
    await userEvent.type(screen.getByLabelText('Valor total'), '1.520,50')
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1))
    expect(onSubmit.mock.calls[0]![0]).toMatchObject({
      customerName: 'Maria Souza',
      description: 'Pedido mensal',
      totalAmount: '1.520,50',
    })
  })

  it('formata o valor em reais enquanto o usuário digita', async () => {
    renderForm()
    const amount = screen.getByLabelText('Valor total')

    await userEvent.type(amount, '100000')
    expect(amount).toHaveValue('1.000,00')

    await userEvent.type(amount, '{Backspace}')
    expect(amount).toHaveValue('100,00')
  })

  it('exibe abaixo do campo o erro devolvido pela API', async () => {
    const serverError = new ApiError(400, {
      status: 400,
      errors: { customerName: ['O nome do cliente deve ter entre 2 e 15 caracteres.'] },
    })

    renderForm({ order, serverError })

    expect(await screen.findByText('O nome do cliente deve ter entre 2 e 15 caracteres.')).toBeInTheDocument()
  })

  it('preenche os campos ao editar e bloqueia em modo leitura', () => {
    renderForm({ order: { ...order, status: 'Paid' }, readOnly: true })

    expect(screen.getByLabelText('Cliente')).toHaveValue('Maria Souza')
    expect(screen.getByLabelText('Valor total')).toHaveValue('1.520,50')
    expect(screen.getByLabelText('Cliente')).toBeDisabled()
    expect(screen.queryByLabelText('Status')).not.toBeInTheDocument()
  })
})

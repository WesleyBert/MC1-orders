import { expect, test } from '@playwright/test'
import { createOrder, payOrder, uniqueName } from './api'

const orderRows = (page: import('@playwright/test').Page) =>
  page.getByRole('row', { name: /Ver detalhes do pedido/ })

test.describe('Cenários reais de uso da tela de pedidos', () => {
  test('1. Lista, busca sem acento, filtro por status e paginação', async ({ page }) => {
    await test.step('Abre a tela com os pedidos de exemplo', async () => {
      await page.goto('/')
      await expect(orderRows(page)).toHaveCount(20)
    })

    await test.step('Busca "joao" e encontra "João" (ignora acentos)', async () => {
      await page.getByRole('searchbox', { name: 'Buscar pedidos' }).fill('joao')
      await expect(page).toHaveURL(/q=joao/)
      await expect(orderRows(page).first()).toContainText('João')
    })

    await test.step('Filtra por status Pago', async () => {
      await page.getByRole('combobox', { name: 'Filtrar por status' }).click()
      await page.getByRole('option', { name: 'Pago' }).click()
      await expect(page).toHaveURL(/status=Paid/)
      const statuses = await orderRows(page).locator('[data-slot="badge"]').allInnerTexts()
      expect(statuses.length).toBeGreaterThan(0)
      expect(statuses.every((status) => status.trim() === 'Pago')).toBe(true)
    })

    await test.step('Vai para a próxima página', async () => {
      await page.getByRole('button', { name: 'Próxima página' }).click()
      await expect(page).toHaveURL(/page=2/)
    })

    await test.step('Recarrega o navegador e mantém busca, filtro e página', async () => {
      await page.reload()
      await expect(page.getByRole('searchbox', { name: 'Buscar pedidos' })).toHaveValue('joao')
      await expect(page).toHaveURL(/page=2/)
      await expect(orderRows(page).first()).toBeVisible()
    })
  })

  test('2. Cria pedido: erros de validação, valor formatado em reais e sucesso', async ({ page }) => {
    const customer = uniqueName('Cliente Playwright')

    await page.goto('/')
    await page.getByRole('button', { name: 'Novo pedido' }).first().click()
    const dialog = page.getByRole('dialog')

    await test.step('Salvar vazio mostra os erros em português', async () => {
      await dialog.getByRole('button', { name: 'Salvar' }).click()
      await expect(dialog.getByText('O nome do cliente é obrigatório.')).toBeVisible()
      await expect(dialog.getByText('A descrição é obrigatória.')).toBeVisible()
      await expect(dialog.getByText('O valor total é obrigatório.')).toBeVisible()
    })

    await test.step('Digitar 152050 formata como 1.520,50', async () => {
      await dialog.getByLabel('Cliente').fill(customer)
      await dialog.getByLabel('Descrição').fill('Reposição de gôndola criada pelo teste')
      await dialog.getByLabel('Valor total').pressSequentially('152050')
      await expect(dialog.getByLabel('Valor total')).toHaveValue('1.520,50')
    })

    await test.step('Salva e o pedido aparece na lista', async () => {
      await dialog.getByRole('button', { name: 'Salvar' }).click()
      await expect(page.getByText(/criado com sucesso/)).toBeVisible()
      await page.getByRole('searchbox', { name: 'Buscar pedidos' }).fill(customer)
      await expect(orderRows(page)).toHaveCount(1)
      await expect(orderRows(page).first()).toContainText('R$ 1.520,50')
      await expect(orderRows(page).first()).toContainText('Aberto')
    })
  })

  test('3. Duas pessoas editam o mesmo pedido: a segunda é avisada e não perde o que digitou', async ({ browser, request, baseURL }, testInfo) => {
    const order = await createOrder(request, uniqueName('Cliente Concorrencia'))
    const newPerson = async () => {
      const context = await browser.newContext({
        baseURL,
        locale: 'pt-BR',
        viewport: { width: 1366, height: 820 },
        recordVideo: { dir: testInfo.outputPath('videos'), size: { width: 1366, height: 820 } },
      })
      return { context, page: await context.newPage() }
    }
    const a = await newPerson()
    const b = await newPerson()
    const personA = a.page
    const personB = b.page

    await test.step('As duas pessoas abrem o mesmo pedido para editar', async () => {
      for (const page of [personA, personB]) {
        await page.goto(`/?q=${order.number}`)
        await page.getByRole('button', { name: `Editar pedido #${order.number}` }).click()
        await expect(page.getByRole('dialog').getByLabel('Descrição')).toBeVisible()
      }
    })

    await test.step('Pessoa A altera e salva primeiro', async () => {
      const dialog = personA.getByRole('dialog')
      await dialog.getByLabel('Descrição').fill('Alterado pela pessoa A')
      await dialog.getByRole('button', { name: 'Salvar' }).click()
      await expect(personA.getByText(`Pedido #${order.number} atualizado.`)).toBeVisible()
    })

    await test.step('Pessoa B tenta salvar com a versão antiga e recebe o aviso', async () => {
      const dialog = personB.getByRole('dialog')
      await dialog.getByLabel('Descrição').fill('Alterado pela pessoa B')
      await dialog.getByRole('button', { name: 'Salvar' }).click()
      await expect(dialog.getByText('Este pedido foi alterado por outra pessoa.')).toBeVisible()
      await expect(dialog.getByLabel('Descrição')).toHaveValue('Alterado pela pessoa B')
      await testInfo.attach('Pessoa B recebe o aviso de conflito', {
        body: await personB.screenshot(),
        contentType: 'image/png',
      })
    })

    await test.step('Pessoa B recarrega e vê a versão salva pela pessoa A', async () => {
      const dialog = personB.getByRole('dialog')
      await dialog.getByRole('button', { name: 'Recarregar dados' }).click()
      await expect(dialog.getByLabel('Descrição')).toHaveValue('Alterado pela pessoa A')
      await testInfo.attach('Pessoa B depois de recarregar', {
        body: await personB.screenshot(),
        contentType: 'image/png',
      })
    })

    for (const [label, person] of [['Pessoa A', a], ['Pessoa B', b]] as const) {
      const video = person.page.video()
      await person.context.close()
      if (video) {
        await testInfo.attach(`Vídeo - ${label}`, { path: await video.path(), contentType: 'video/webm' })
      }
    }
  })

  test('4. A lista se atualiza sozinha em até 10 segundos, sem recarregar a página', async ({ page, request }) => {
    await page.goto('/')
    await expect(orderRows(page)).toHaveCount(20)
    await page.evaluate(() => {
      ;(window as unknown as { semRecarregar: boolean }).semRecarregar = true
    })

    const customer = uniqueName('Criado por outra pessoa')

    await test.step('Outra pessoa cria um pedido pela API', async () => {
      await createOrder(request, customer)
    })

    await test.step('O pedido aparece na tela sozinho, em até 12 segundos', async () => {
      await expect(page.getByText(customer)).toBeVisible({ timeout: 12_000 })
    })

    await test.step('A página não foi recarregada', async () => {
      const notReloaded = await page.evaluate(() => (window as unknown as { semRecarregar?: boolean }).semRecarregar)
      expect(notReloaded).toBe(true)
    })
  })

  test('5. Pedido pago: somente leitura e exclusão bloqueada', async ({ page, request }) => {
    const order = await createOrder(request, uniqueName('Cliente Pago'))
    await payOrder(request, order)

    await page.goto(`/?q=${order.number}`)
    await expect(orderRows(page)).toHaveCount(1)
    await expect(orderRows(page).first()).toContainText('Pago')

    await test.step('O botão de excluir aparece bloqueado', async () => {
      await expect(page.getByRole('button', { name: 'Exclusão indisponível' })).toBeDisabled()
    })

    await test.step('Ao abrir, a tela avisa que pedido pago não pode ser alterado', async () => {
      await page.getByRole('button', { name: `Ver pedido #${order.number}` }).click()
      const dialog = page.getByRole('dialog')
      await expect(dialog.getByText('Pedidos com status Pago não podem ser alterados.')).toBeVisible()
      await expect(dialog.getByRole('button', { name: 'Salvar' })).toHaveCount(0)
    })
  })

  test('6. Exclui pedido com confirmação', async ({ page, request }) => {
    const order = await createOrder(request, uniqueName('Cliente Excluir'))

    await page.goto(`/?q=${order.number}`)
    await page.getByRole('button', { name: `Excluir pedido #${order.number}` }).click()

    await test.step('Pede confirmação antes de excluir', async () => {
      const confirm = page.getByRole('alertdialog')
      await expect(confirm).toContainText('Esta ação não pode ser desfeita.')
      await confirm.getByRole('button', { name: 'Excluir' }).click()
    })

    await test.step('Mostra o aviso e o pedido some da lista', async () => {
      await expect(page.getByText(`Pedido #${order.number} excluído.`)).toBeVisible()
      await expect(orderRows(page)).toHaveCount(0)
    })
  })
})

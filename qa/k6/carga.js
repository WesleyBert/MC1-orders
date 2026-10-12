import http from 'k6/http'
import { check, group, sleep } from 'k6'
import { Counter } from 'k6/metrics'

const BASE = __ENV.BASE_URL || 'http://localhost:8080'
const JSON_HEADERS = { 'Content-Type': 'application/json' }
const SEARCH_TERMS = ['joao', 'maria', 'silva', 'reposicao', 'bebidas', 'mensal']
const STATUSES = ['', 'Open', 'Paid', 'Cancelled']
const SORTS = ['createdAt', 'number', 'customerName', 'totalAmount', 'status']

const conflictsDetected = new Counter('conflitos_detectados')
const lostUpdates = new Counter('gravacoes_perdidas')

http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 412))

export const options = {
  scenarios: {
    navegacao: {
      executor: 'ramping-vus',
      exec: 'browse',
      startVUs: 0,
      stages: [
        { duration: '15s', target: 80 },
        { duration: '45s', target: 80 },
        { duration: '10s', target: 0 },
      ],
    },
    cadastro_e_edicao: {
      executor: 'ramping-vus',
      exec: 'edit',
      startVUs: 0,
      stages: [
        { duration: '15s', target: 20 },
        { duration: '45s', target: 20 },
        { duration: '10s', target: 0 },
      ],
    },
    duas_pessoas_mesmo_pedido: {
      executor: 'constant-vus',
      exec: 'conflict',
      vus: 5,
      duration: '60s',
      startTime: '5s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{tipo:leitura}': ['p(95)<300'],
    'http_req_duration{tipo:escrita}': ['p(95)<500'],
    checks: ['rate>0.99'],
    conflitos_detectados: ['count>0'],
    gravacoes_perdidas: ['count==0'],
  },
}

const pick = (list) => list[Math.floor(Math.random() * list.length)]

function newOrder(prefix) {
  return {
    customerName: `${prefix} ${__VU}-${__ITER}`,
    description: 'Pedido criado no teste de carga',
    totalAmount: Math.round(Math.random() * 500000 + 100) / 100,
  }
}

function createOrder(prefix) {
  const response = http.post(`${BASE}/api/v1/orders`, JSON.stringify(newOrder(prefix)), {
    headers: JSON_HEADERS,
    tags: { tipo: 'escrita', name: 'POST /orders' },
  })
  check(response, { 'criar pedido responde 201': (r) => r.status === 201 })
  return response.status === 201 ? response.json() : null
}

function updateBody(order, description, status) {
  return JSON.stringify({
    customerName: order.customerName,
    description,
    totalAmount: order.totalAmount,
    status,
  })
}

export function browse() {
  group('Navegar pela lista', () => {
    const params = [
      `page=${1 + Math.floor(Math.random() * 20)}`,
      `pageSize=${pick([10, 20, 50])}`,
      `sortBy=${pick(SORTS)}`,
      `sortDir=${pick(['asc', 'desc'])}`,
    ]
    if (Math.random() < 0.5) params.push(`search=${pick(SEARCH_TERMS)}`)
    const status = pick(STATUSES)
    if (status) params.push(`status=${status}`)

    const list = http.get(`${BASE}/api/v1/orders?${params.join('&')}`, {
      tags: { tipo: 'leitura', name: 'GET /orders' },
    })
    check(list, {
      'listar responde 200': (r) => r.status === 200,
      'listar devolve no máximo 50 itens': (r) => r.json('items').length <= 50,
    })

    const summary = http.get(`${BASE}/api/v1/orders/summary`, {
      tags: { tipo: 'leitura', name: 'GET /orders/summary' },
    })
    check(summary, { 'resumo responde 200': (r) => r.status === 200 })
  })
  sleep(1)
}

export function edit() {
  group('Criar, consultar, editar e excluir', () => {
    const order = createOrder('Carga')
    if (!order) return

    const loaded = http.get(`${BASE}/api/v1/orders/${order.id}`, {
      tags: { tipo: 'leitura', name: 'GET /orders/{id}' },
    })
    check(loaded, { 'consultar responde 200 com ETag': (r) => r.status === 200 && r.headers.Etag === '"1"' })

    const updated = http.put(`${BASE}/api/v1/orders/${order.id}`, updateBody(order, 'Editado no teste de carga', 'Open'), {
      headers: { ...JSON_HEADERS, 'If-Match': '"1"' },
      tags: { tipo: 'escrita', name: 'PUT /orders/{id}' },
    })
    check(updated, { 'editar responde 200 e vai para versão 2': (r) => r.status === 200 && r.json('version') === 2 })

    if (Math.random() < 0.5) {
      const paid = http.put(`${BASE}/api/v1/orders/${order.id}`, updateBody(order, 'Pago no teste de carga', 'Paid'), {
        headers: { ...JSON_HEADERS, 'If-Match': '"2"' },
        tags: { tipo: 'escrita', name: 'PUT /orders/{id}' },
      })
      check(paid, { 'pagar responde 200': (r) => r.status === 200 && r.json('status') === 'Paid' })
    } else {
      const removed = http.del(`${BASE}/api/v1/orders/${order.id}`, null, {
        headers: { 'If-Match': '"2"' },
        tags: { tipo: 'escrita', name: 'DELETE /orders/{id}' },
      })
      check(removed, { 'excluir responde 204': (r) => r.status === 204 })
    }
  })
  sleep(1)
}

export function conflict() {
  group('Duas pessoas salvam o mesmo pedido ao mesmo tempo', () => {
    const order = createOrder('Conflito')
    if (!order) return

    const save = (who) => [
      'PUT',
      `${BASE}/api/v1/orders/${order.id}`,
      updateBody(order, `Salvo pela pessoa ${who}`, 'Open'),
      { headers: { ...JSON_HEADERS, 'If-Match': '"1"' }, tags: { tipo: 'escrita', name: 'PUT /orders/{id} (conflito)' } },
    ]
    const [first, second] = http.batch([save('A'), save('B')])
    const statuses = [first.status, second.status].sort()

    const exactlyOneWon = statuses[0] === 200 && statuses[1] === 412
    if (exactlyOneWon) conflictsDetected.add(1)
    if (statuses[0] === 200 && statuses[1] === 200) lostUpdates.add(1)

    check(null, { 'só uma das duas gravações passa (200 + 412)': () => exactlyOneWon })

    const final = http.get(`${BASE}/api/v1/orders/${order.id}`, {
      tags: { tipo: 'leitura', name: 'GET /orders/{id}' },
    })
    check(final, { 'o pedido fica na versão 2, sem gravação perdida': (r) => r.json('version') === 2 })
  })
  sleep(0.5)
}

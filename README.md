# MC1 Orders — Cadastro de Pedidos

Desafio técnico para Desenvolvedor(a) Web .NET Pleno: API REST em ASP.NET Core (.NET 10), página web em React
e execução com Docker. Persistência em memória, segura para requisições simultâneas, com 10.000 pedidos
gerados na inicialização.

## Como rodar

**Com Docker (um comando):**

```bash
docker compose up --build
```

| URL | O que é |
|---|---|
| http://localhost:8080 | Aplicação web |
| http://localhost:8080/scalar/v1 | Documentação interativa da API (OpenAPI) |
| http://localhost:8080/health/ready | Prontidão (responde após a carga inicial) |

**Sem Docker (desenvolvimento)** — requer .NET 10 SDK e Node 22:

```bash
dotnet run --project src/Orders.Api
```

```bash
cd web && npm install && npm run dev
```

API em http://localhost:5080 e front com hot reload em http://localhost:5173 (o Vite repassa `/api` para a API).

**Testes:**

```bash
dotnet test
```

```bash
cd web && npm test
```

**Configuração** (variáveis de ambiente, nenhuma é segredo):

| Variável | Padrão | Uso |
|---|---|---|
| `Seed__Count` | `10000` | Quantidade de pedidos gerados |
| `Seed__RandomSeed` | `42` | Semente (dados reproduzíveis) |
| `Seed__Enabled` | `true` | Liga/desliga a carga inicial |
| `Api__EnableDocs` | `true` | Publica OpenAPI e Scalar |

## O que foi entregue

- **API** `/api/v1/orders`: criar, listar (busca, filtro por status, ordenação e paginação), obter, atualizar e excluir.
- **Concorrência**: escritas atômicas sem lock global e concorrência otimista com `ETag`/`If-Match`.
- **Validação** com mensagens em pt-BR por campo, no formato `ProblemDetails` (RFC 9457) com código estável.
- **Tela** que lista, busca, filtra, ordena, pagina, cria, edita e exclui, atualizando sozinha a cada 10 segundos.
- **Docker** multi-stage, usuário não-root e healthcheck; **CI** no GitHub Actions; **211 testes** automatizados.

## Arquitetura

```
src/
  Orders.Domain/          Entidade Order, enum de status, máquina de estados, erros de negócio
  Orders.Application/     Casos de uso (OrderService), contratos da API, validadores, interface do repositório
  Orders.Infrastructure/  Repositório em memória, modelo de persistência, carga inicial
  Orders.Api/             Controller, ProblemDetails, health checks, OpenAPI, hospedagem da SPA
web/                      React + TypeScript (Vite), build servido pela própria API
tests/                    Unitários (domínio, aplicação, concorrência) e de integração (HTTP)
```

As dependências apontam para o domínio: `Api → Application → Domain` e `Infrastructure → Application`.
Trocar a memória por um banco é escrever outro `IOrderRepository`, sem tocar em domínio ou casos de uso.

| Método | Rota | Sucesso | Erros |
|---|---|---|---|
| GET | `/api/v1/orders?search=&status=&page=&pageSize=&sortBy=&sortDir=` | 200 | 400 |
| GET | `/api/v1/orders/{id}` | 200 + `ETag` | 404 |
| POST | `/api/v1/orders` | 201 + `Location` + `ETag` | 400 |
| PUT | `/api/v1/orders/{id}` (header `If-Match` opcional) | 200 + `ETag` | 400, 404, 409, 412 |
| DELETE | `/api/v1/orders/{id}` (header `If-Match` opcional) | 204 | 404, 409, 412 |

## Decisões

**Regras de negócio (premissas minhas).** Um pedido nasce *Aberto*. *Aberto* pode ir para *Pago* ou
*Cancelado*; esses dois são finais e não podem mais ser alterados. Pedido *Pago* não pode ser excluído
(é registro financeiro). Além dos campos pedidos, adicionei `number` (número legível, ex.: #10234),
`updatedAt` e `version` (controle de concorrência).

**Persistência em memória e concorrência.** `ConcurrentDictionary` guardando um modelo de persistência
imutável (`OrderRecord`). Cada leitura devolve uma entidade nova; a gravação só acontece se a versão
armazenada ainda for a lida (compare-and-swap atômico), como um `rowversion` de banco. Sem `If-Match`,
o serviço relê e reaplica as regras (até 3 tentativas): pagar e cancelar o mesmo pedido ao mesmo tempo
nunca passam juntos. Com `If-Match` divergente, a resposta é 412 e nada é sobrescrito. Há testes com
milhares de operações paralelas cobrindo esses casos.

**Domínio.** `Order` é uma entidade (`Entity<Guid>`) com setters privados e comportamento (`Create`,
`Update`, `EnsureCanDelete`); a máquina de estados fica em uma tabela única. Erros de negócio voltam
como `Result`, não como exceção. Não usei MediatR/CQRS nem value objects: para cinco operações seria
indireção sem ganho.

**Busca e listagem.** A busca ignora maiúsculas e acentos ("joao" encontra "João") e usa uma chave
normalizada calculada na gravação, não a cada listagem — importante com o refresh de 10 s. A ordenação
desempata pelo número do pedido para a paginação ser estável.

**Carga inicial.** Bogus (pt-BR) com semente fixa, executada antes de a API aceitar requisições; a
readiness só fica saudável depois dela. Leva cerca de 250 ms para 10.000 pedidos.

**Erros.** Toda resposta de erro é `application/problem+json` com `code` (ex.: `order.immutable_state`,
`order.concurrency_conflict`) e `traceId`; o front decide pelo código, não pelo texto. Erro 500 nunca
expõe detalhes internos.

**Front.** React + TypeScript, TanStack Query (polling de 10 s mantendo os dados na tela, sem piscar),
react-hook-form + zod (mesmas regras da API, e os erros da API aparecem no campo), Tailwind + shadcn/ui.
Filtros e página ficam na URL. Ao salvar ou excluir, o front envia `If-Match`; em conflito, avisa e
oferece recarregar sem perder o que foi digitado.

**Hospedagem.** O front é servido pela própria API (mesma origem, sem CORS, um único container). Não é
um BFF: a API é genérica e não há agregação nem autenticação. Se surgir login OAuth ou vários serviços,
eu colocaria um BFF (YARP) guardando os tokens no servidor.

**Docker e CI.** Imagem multi-stage (Node → SDK → runtime), usuário não-root, healthcheck usando o próprio
binário. Os testes rodam no CI, não no build da imagem, para manter o `docker compose up` rápido.

## Limitações conhecidas

- Os dados ficam só na memória: reiniciar a aplicação recria a base a partir da carga inicial.
- Funciona em uma instância só; duas réplicas teriam bases diferentes.
- Sem autenticação (fora do escopo do desafio).
- `If-Match` é opcional na API para facilitar testes via curl/Scalar; o front sempre envia.
- O bundle do front tem cerca de 200 KB gzip, aceitável para backoffice, mas sem divisão por rota.

## O que eu faria com mais tempo

- PostgreSQL com EF Core (`xmin` como token de concorrência), migrations e índice `pg_trgm` para a busca.
- Autenticação JWT/OAuth2 e autorização por perfil.
- Endpoints de ação (`POST /orders/{id}/pay` e `/cancel`) e `Idempotency-Key` no POST.
- Paginação por cursor para volumes grandes.
- Testes end-to-end com Playwright e OpenTelemetry para traces e métricas.
- Imagem *chiseled* para reduzir o container (hoje ~350 MB) e pipeline também no Azure DevOps.

## Uso de IA

Descrito em [AI_USAGE.md](AI_USAGE.md).

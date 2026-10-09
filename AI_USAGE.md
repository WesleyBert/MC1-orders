# Uso de IA

## Ferramentas e onde usei

Usei o **Claude Code** (app desktop, modelo Claude Opus 5.5) em todas as etapas: leitura do enunciado,
especificação, implementação do back e do front, testes, Docker e documentação. Trabalhei em fases curtas
(domínio → aplicação → infraestrutura → API → front → entrega), revisando cada uma, rodando os testes e
fazendo um commit por fase antes de seguir.

## Prompts mais úteis e como montei o contexto

1. **Contexto completo e plano antes de código.** Passei o PDF do desafio *e* a descrição da vaga
   (ASP.NET Web API, EF, SQL Server/Postgres, filas, Docker, Azure DevOps) e pedi primeiro só o
   planejamento. Com a vaga no contexto, as escolhas foram justificadas pela realidade do time
   (Controllers, xUnit, Docker), e não por preferência genérica.
2. **"Se comporte como um arquiteto… especifique todos os pontos de contato, a máquina de estados com
   o de-para dos campos e os casos de uso."** Isso gerou uma especificação (mantida fora do repositório)
   com regras de negócio, contratos HTTP, catálogo de erros e casos de teste. Ela virou o roteiro das
   fases e a base dos testes: cada cenário da especificação virou um teste.
3. **Correções de padrão de código**, por exemplo: "separe as models, use entidade na regra de negócio
   seguindo boas práticas e remova os comentários". Prompts curtos e diretos sobre o resultado esperado
   funcionaram melhor do que descrever a implementação.

## O que aceitei, corrigi e descartei

- **Aceitei:** arquitetura em camadas com dependências apontando para o domínio; concorrência otimista
  com compare-and-swap e `ETag`/`If-Match`; erros em `ProblemDetails` com código estável; TanStack Query
  para o refresh de 10 s.
- **Corrigi:** a primeira versão agrupava vários tipos por arquivo e modelava o pedido como `record`
  imutável. Pedi um tipo por arquivo, pastas por responsabilidade e uma entidade real (`Entity<TId>`,
  setters privados, comportamento). Para manter a concorrência segura com entidade mutável, o repositório
  passou a guardar um modelo de persistência separado e a gravar só se a versão não mudou.
- **Descartei:** MediatR/CQRS e value objects, por serem indireção sem ganho para cinco operações.

## Erros da IA que identifiquei

- **Bugs no pipeline HTTP pegos pelos testes de integração:** um `[Produces("application/json")]` no
  controller sobrescrevia o `application/problem+json` dos erros, e uma rota de fallback transformava
  405 e 415 em 404. 7 de 38 testes falharam; corrigi a causa, não os testes.
- **Página inicial com 404:** o fallback da SPA usava uma regex com `?`, que tem outro significado nos
  templates de rota do ASP.NET. Os testes não pegaram porque não havia `wwwroot` no teste. Percebi ao
  subir a aplicação de verdade; troquei por uma restrição de rota própria e adicionei testes com um
  `index.html` real.
- **Teste com premissa errada e alarme falso:** um teste comparava pedidos com GUIDs aleatórios
  diferentes (o erro estava no teste, não no código), e a IA chamou de "risco de supply chain" um pacote
  que, conferindo o repositório, era oficial do shadcn. Lição: conferir antes de aceitar, inclusive
  afirmações da IA.

## Como validei

Testes escritos a partir da especificação (174 no .NET e 37 no front), incluindo concorrência real com
milhares de operações em paralelo, rodados várias vezes para descartar resultado instável. Além disso,
usei a tela no navegador: editei um pedido enquanto outra requisição o alterava (conflito detectado sem
perder o que foi digitado), criei pedidos pela API e conferi que apareciam sozinhos em até 10 s, e subi
tudo com `docker compose up --build` como um avaliador faria.

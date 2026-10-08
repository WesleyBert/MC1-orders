using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.Application.Orders;
using Orders.Application.Orders.Contracts;
using Orders.Application.Orders.Persistence;
using Orders.Application.Orders.Validators;
using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Infrastructure.Orders;
using Orders.Infrastructure.Seeding;

namespace Orders.UnitTests.Infrastructure;

/// <summary>
/// Concorrência real: muitas tarefas em paralelo contra o repositório em memória e o serviço,
/// sem simulação. Cada cenário é repetido para aumentar a chance de expor corridas.
/// </summary>
public sealed class ConcurrencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryOrderRepository _repository = new();
    private readonly OrderService _service;

    public ConcurrencyTests()
    {
        _service = new OrderService(
            _repository,
            new CreateOrderRequestValidator(),
            new UpdateOrderRequestValidator(),
            new ListOrdersQueryValidator(),
            TimeProvider.System,
            NullLogger<OrderService>.Instance);
    }

    private static readonly ParallelOptions Parallelism = new() { MaxDegreeOfParallelism = Environment.ProcessorCount * 4 };

    private async Task<OrderResponse> CreateAsync() =>
        (await _service.CreateAsync(new CreateOrderRequest("Maria Souza", "Reposição de gôndola", 100m), default)).Value;

    private static UpdateOrderRequest Update(string status, decimal amount = 100m) =>
        new("Maria Souza", "Reposição de gôndola", amount, status);

    [Fact]
    public async Task ParallelCreates_OnTopOfSeed_KeepEveryOrderWithUniqueNumbers()
    {
        foreach (var order in OrderSeeder.Generate(10_000, 42, Now, _repository.NextNumber))
        {
            await _repository.AddAsync(order, default);
        }

        var created = new ConcurrentBag<long>();
        await Parallel.ForEachAsync(Enumerable.Range(0, 1_000), Parallelism, async (_, ct) =>
            created.Add((await _service.CreateAsync(
                new CreateOrderRequest("Cliente Paralelo", "Pedido concorrente", 10m), ct)).Value.Number));

        _repository.Count.Should().Be(11_000);
        created.Should().OnlyHaveUniqueItems();
        created.Should().BeEquivalentTo(Enumerable.Range(20_001, 1_000).Select(n => (long)n));
    }

    // T10: Pay ∥ Cancel no mesmo pedido Open, sem If-Match. Exatamente um vence.
    [Fact]
    public async Task ConflictingTransitions_ExactlyOneWins()
    {
        for (var round = 0; round < 200; round++)
        {
            var order = await CreateAsync();
            using var start = new Barrier(2);

            var results = await Task.WhenAll(
                Task.Run(() => { start.SignalAndWait(); return _service.UpdateAsync(order.Id, Update("Paid"), null, default); }),
                Task.Run(() => { start.SignalAndWait(); return _service.UpdateAsync(order.Id, Update("Cancelled"), null, default); }));

            results.Count(r => r.IsSuccess).Should().Be(1, $"rodada {round}");
            results.Single(r => !r.IsSuccess).Error!.Code.Should().Be("order.immutable_state");

            var final = (await _repository.GetAsync(order.Id, default))!;
            final.Version.Should().Be(2, "apenas uma transição foi gravada");
            final.Status.Should().Be(results.Single(r => r.IsSuccess).Value.Status);
        }
    }

    [Fact]
    public async Task ParallelUpdates_WithSameIfMatch_OnlyOneSucceeds()
    {
        var order = await CreateAsync();
        var results = new ConcurrentBag<Result<OrderResponse>>();

        await Parallel.ForEachAsync(Enumerable.Range(1, 100), Parallelism, async (i, ct) =>
            results.Add(await _service.UpdateAsync(order.Id, Update("Open", amount: i), expectedVersion: 1, ct)));

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Where(r => !r.IsSuccess).Should().AllSatisfy(r => r.Error.Should().Be(OrderErrors.VersionMismatch));
        (await _repository.GetAsync(order.Id, default))!.Version.Should().Be(2);
    }

    // Sem If-Match, cada edição é reaplicada sobre a última versão: nenhuma se perde
    // (a não ser que a contenção persista por 3 tentativas, o que vira 409 explícito).
    [Fact]
    public async Task ParallelUpdates_WithoutIfMatch_NoSilentLostUpdates()
    {
        var order = await CreateAsync();
        var results = new ConcurrentBag<Result<OrderResponse>>();

        await Parallel.ForEachAsync(Enumerable.Range(1, 200), Parallelism, async (i, ct) =>
            results.Add(await _service.UpdateAsync(order.Id, Update("Open", amount: i), null, ct)));

        var successes = results.Count(r => r.IsSuccess);
        results.Where(r => !r.IsSuccess).Should().AllSatisfy(r => r.Error.Should().Be(OrderErrors.ConcurrencyConflict));

        var final = (await _repository.GetAsync(order.Id, default))!;
        final.Version.Should().Be(1 + successes, "cada sucesso reportado corresponde a exatamente uma versão gravada");
    }

    [Fact]
    public async Task ConcurrentDeleteAndPay_NeverDeletesPaidOrder()
    {
        for (var round = 0; round < 200; round++)
        {
            var order = await CreateAsync();
            using var start = new Barrier(2);

            var pay = Task.Run(() => { start.SignalAndWait(); return _service.UpdateAsync(order.Id, Update("Paid"), null, default); });
            var delete = Task.Run(() => { start.SignalAndWait(); return _service.DeleteAsync(order.Id, null, default); });
            var payResult = await pay;
            var deleteResult = await delete;

            var stored = await _repository.GetAsync(order.Id, default);
            if (payResult.IsSuccess)
            {
                stored.Should().NotBeNull($"rodada {round}: pedido pago não pode sumir");
                stored!.Status.Should().Be(OrderStatus.Paid);
                deleteResult.Error.Should().Be(OrderErrors.DeleteNotAllowed);
            }
            else
            {
                stored.Should().BeNull();
                deleteResult.IsSuccess.Should().BeTrue();
                payResult.Error.Should().Be(OrderErrors.NotFound);
            }
        }
    }

    [Fact]
    public async Task ListingsDuringWrites_AlwaysSeeConsistentOrders()
    {
        foreach (var order in OrderSeeder.Generate(2_000, 7, Now, _repository.NextNumber))
        {
            await _repository.AddAsync(order, default);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var ids = (await _repository.ListAsync(
            new OrderListCriteria(null, OrderStatus.Open, 1, 100, OrderSortField.Number, false), default)).Items
            .Select(o => o.Id).ToArray();

        var writers = Task.Run(async () =>
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                var id = ids[i++ % ids.Length];
                await _service.UpdateAsync(id, Update("Open", amount: (i % 500) + 1), null, default);
                await _service.CreateAsync(new CreateOrderRequest("Escritor", "Pedido durante leitura", 1m), default);
            }
        });

        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            var reads = 0;
            while (!cts.IsCancellationRequested)
            {
                var page = await _repository.ListAsync(
                    new OrderListCriteria("pedido", null, 1, 100, OrderSortField.CreatedAt, true), default);

                page.Items.Should().OnlyHaveUniqueItems(o => o.Id);
                page.Items.Should().AllSatisfy(o =>
                {
                    o.Version.Should().BePositive();
                    o.TotalAmount.Should().BePositive();
                });
                page.TotalItems.Should().BeGreaterThanOrEqualTo(page.Items.Count);
                reads++;
            }

            return reads;
        })).ToArray();

        await writers;
        (await Task.WhenAll(readers)).Should().AllSatisfy(r => r.Should().BePositive());
    }
}

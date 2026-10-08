using System.Diagnostics;
using Bogus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orders.Application.Orders.Persistence;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Seeding;

/// <summary>
/// Gera a carga inicial de pedidos. Roda em <see cref="StartAsync"/>, que o host executa
/// antes de o servidor HTTP aceitar requisições: nenhuma consulta vê a base pela metade.
/// </summary>
public sealed partial class OrderSeeder(
    IOrderRepository repository,
    IOptions<SeedOptions> options,
    SeedStatus status,
    TimeProvider timeProvider,
    ILogger<OrderSeeder> logger) : IHostedService
{
    private static readonly string[] Actions =
    [
        "Reposição de gôndola", "Pedido mensal", "Ação promocional", "Bonificação",
        "Troca de mix", "Abastecimento de ponto extra", "Pedido de lançamento", "Reposição emergencial",
    ];

    private static readonly string[] Categories =
    [
        "linha bebidas", "higiene pessoal", "limpeza", "snacks", "laticínios",
        "mercearia seca", "congelados", "pet care", "bomboniere",
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            status.MarkCompleted();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        foreach (var order in Generate(settings.Count, settings.RandomSeed, timeProvider.GetUtcNow(), repository.NextNumber))
        {
            await repository.AddAsync(order, cancellationToken);
        }

        status.MarkCompleted();
        LogSeedCompleted(settings.Count, stopwatch.ElapsedMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Gera pedidos determinísticos para uma semente. Números são atribuídos em ordem de criação,
    /// e pedidos finais passam pela máquina de estados do domínio (versão 2), como na vida real.
    /// </summary>
    public static IEnumerable<Order> Generate(int count, int seed, DateTimeOffset now, Func<long> nextNumber)
    {
        // Randomizer próprio: não altera o estado global do Bogus (Randomizer.Seed).
        var faker = new Faker("pt_BR") { Random = new Randomizer(seed) };

        var drafts = Enumerable.Range(0, count)
            .Select(_ => NewDraft(faker, now))
            .OrderBy(d => d.CreatedAt)
            .ToList();

        foreach (var draft in drafts)
        {
            var order = Order.Create(nextNumber(), draft.CustomerName, draft.Description, draft.TotalAmount, draft.CreatedAt);

            yield return draft.Status == OrderStatus.Open
                ? order
                : order.Update(order.CustomerName, order.Description, order.TotalAmount, draft.Status, draft.UpdatedAt).Value;
        }
    }

    private static Draft NewDraft(Faker f, DateTimeOffset now)
    {
        var createdAt = f.Date.BetweenOffset(now.AddDays(-180), now);
        var status = f.Random.WeightedRandom(
            [OrderStatus.Open, OrderStatus.Paid, OrderStatus.Cancelled], [0.6f, 0.3f, 0.1f]);
        var closedAt = createdAt.AddMinutes(f.Random.Int(60, 15 * 24 * 60));

        return new Draft(
            CustomerName: f.Random.Bool() ? f.Name.FullName() : f.Company.CompanyName(),
            Description: $"{f.PickRandom(Actions)} — {f.PickRandom(Categories)}",
            TotalAmount: Math.Round(f.Random.Decimal(10m, 50_000m), 2),
            Status: status,
            CreatedAt: createdAt,
            UpdatedAt: closedAt < now ? closedAt : now);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Carga inicial concluída: {Count} pedidos em {ElapsedMs} ms")]
    private partial void LogSeedCompleted(int count, long elapsedMs);

    private sealed record Draft(
        string CustomerName,
        string Description,
        decimal TotalAmount,
        OrderStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Orders.Application.Orders.Contracts;
using Orders.Application.Orders.Validators;
using Orders.Domain.Orders;
using Orders.Infrastructure.Orders;
using Orders.Infrastructure.Seeding;

namespace Orders.UnitTests.Infrastructure;

public sealed class OrderSeederTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    private static List<Order> Generate(int count = 10_000, int seed = 42)
    {
        long number = InMemoryOrderRepository.FirstNumber - 1;
        return OrderSeeder.Generate(count, seed, Now, () => ++number).ToList();
    }

    [Fact]
    public async Task StartAsync_LoadsConfiguredCountAndMarksCompleted()
    {
        var repository = new InMemoryOrderRepository();
        var status = new SeedStatus();
        var seeder = new OrderSeeder(
            repository, Options.Create(new SeedOptions()), status, new FakeTimeProvider(Now), NullLogger<OrderSeeder>.Instance);

        await seeder.StartAsync(default);

        repository.Count.Should().Be(10_000);
        status.IsCompleted.Should().BeTrue();
        repository.NextNumber().Should().Be(20_001);
    }

    [Fact]
    public async Task StartAsync_Disabled_LoadsNothingButIsReady()
    {
        var repository = new InMemoryOrderRepository();
        var status = new SeedStatus();
        var seeder = new OrderSeeder(
            repository, Options.Create(new SeedOptions { Enabled = false }), status, TimeProvider.System, NullLogger<OrderSeeder>.Instance);

        await seeder.StartAsync(default);

        repository.Count.Should().Be(0);
        status.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Generate_SameSeed_ProducesSameData()
    {
        static object Shape(Order o) => new { o.Number, o.CustomerName, o.Description, o.TotalAmount, o.Status, o.CreatedAt };

        Generate(500).Select(Shape).Should().Equal(Generate(500).Select(Shape));
        Generate(500, seed: 1).Select(Shape).Should().NotEqual(Generate(500).Select(Shape));
    }

    [Fact]
    public void Generate_EveryOrderPassesApiValidation()
    {
        var validator = new UpdateOrderRequestValidator();

        Generate().Should().AllSatisfy(o =>
            validator.Validate(new UpdateOrderRequest(o.CustomerName, o.Description, o.TotalAmount, o.Status.ToString()))
                .IsValid.Should().BeTrue());
    }

    [Fact]
    public void Generate_NumbersFollowCreationOrder()
    {
        var orders = Generate();

        orders.Select(o => o.Number).Should().Equal(Enumerable.Range(10_001, 10_000).Select(n => (long)n));
        orders.Select(o => o.CreatedAt).Should().BeInAscendingOrder();
        orders.Should().AllSatisfy(o => o.CreatedAt.Should().BeOnOrAfter(Now.AddDays(-180)).And.BeOnOrBefore(Now));
    }

    [Fact]
    public void Generate_StatusDistributionIsRoughly60_30_10()
    {
        var byStatus = Generate().GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count() / 10_000.0);

        byStatus[OrderStatus.Open].Should().BeApproximately(0.6, 0.03);
        byStatus[OrderStatus.Paid].Should().BeApproximately(0.3, 0.03);
        byStatus[OrderStatus.Cancelled].Should().BeApproximately(0.1, 0.03);
    }

    [Fact]
    public void Generate_FinalOrdersWentThroughStateMachine()
    {
        Generate().Should().AllSatisfy(o =>
        {
            o.UpdatedAt.Should().BeOnOrAfter(o.CreatedAt).And.BeOnOrBefore(Now);
            o.Version.Should().Be(o.Status == OrderStatus.Open ? 1 : 2);
            o.TotalAmount.Should().BeInRange(10m, 50_000m);
        });
    }
}

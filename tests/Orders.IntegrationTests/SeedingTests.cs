using System.Diagnostics;
using System.Net;
using static Orders.IntegrationTests.HttpExtensions;

namespace Orders.IntegrationTests;

public sealed class FullSeedFactory : OrdersApiFactory
{
    protected override int Seed => 10_000;
}

public sealed class SeedingTests(FullSeedFactory factory) : IClassFixture<FullSeedFactory>
{
    [Fact]
    public async Task FirstRequest_SeesAllTenThousandOrders()
    {
        using var client = factory.CreateClient();

        var body = await (await client.GetAsync(Url("/api/v1/orders"))).JsonAsync();

        body.GetProperty("totalItems").GetInt32().Should().Be(10_000);
        body.GetProperty("totalPages").GetInt32().Should().Be(500);
        body.GetProperty("items")[0].GetProperty("number").GetInt64().Should().Be(20_000);
    }

    [Fact]
    public async Task SearchOverFullBase_IsFast()
    {
        using var client = factory.CreateClient();
        await client.GetAsync(Url("/api/v1/orders?search=reposicao"));
        var timings = new List<double>();

        for (var i = 0; i < 100; i++)
        {
            var started = Stopwatch.GetTimestamp();
            var response = await client.GetAsync(Url($"/api/v1/orders?search=reposicao&page={(i % 10) + 1}&sortBy=totalAmount"));
            timings.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        timings.Order().ElementAt(94).Should().BeLessThan(50);
    }
}

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Orders.Persistence;
using static Orders.IntegrationTests.HttpExtensions;

namespace Orders.IntegrationTests;

public sealed class PlatformTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthChecks_AreHealthy(string path)
    {
        var response = await _client.GetAsync(Url(path));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task OpenApiDocument_DescribesOrderEndpoints()
    {
        var document = await (await _client.GetAsync(Url("/openapi/v1.json"))).JsonAsync();

        document.GetProperty("info").GetProperty("title").GetString().Should().Be("MC1 Orders API");
        var paths = document.GetProperty("paths");
        paths.TryGetProperty("/api/v1/orders", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/orders/{id}", out var item).Should().BeTrue();
        item.TryGetProperty("put", out _).Should().BeTrue();
        item.TryGetProperty("delete", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ScalarUi_IsServed()
    {
        (await _client.GetAsync(Url("/scalar/v1"))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        var response = await _client.GetAsync(Url("/health/live"));

        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle("no-referrer");
    }

    [Fact]
    public async Task UnknownApiRoute_Returns404Problem()
    {
        var response = await _client.GetAsync(Url("/api/v1/unknown"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await response.JsonAsync()).Code().Should().Be("resource.not_found");
    }

    [Fact]
    public async Task UnsupportedMethod_Returns405Problem()
    {
        var response = await _client.PatchAsync(Url("/api/v1/orders"), RawJson("{}"));

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await response.JsonAsync()).Code().Should().Be("request.method_not_allowed");
    }

    [Fact]
    public async Task OversizedBody_Returns413Problem()
    {
        var json = $"{{\"customerName\":\"{new string('a', 70_000)}\"}}";

        var response = await _client.PostAsync(Url("/api/v1/orders"), RawJson(json));

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        (await response.JsonAsync()).Code().Should().Be("request.too_large");
    }

    [Fact]
    public async Task UnexpectedError_Returns500WithoutInternals()
    {
        using var failing = factory.WithWebHostBuilder(builder => builder
            .UseSetting("Seed:Enabled", "false")
            .ConfigureTestServices(services => services.AddSingleton<IOrderRepository, ThrowingOrderRepository>()));
        using var client = failing.CreateClient();

        var response = await client.GetAsync(Url("/api/v1/orders"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(ThrowingOrderRepository.SecretMessage);
        var problem = await response.JsonAsync();
        problem.Code().Should().Be("server.unexpected");
        problem.GetProperty("detail").GetString().Should().Be("Ocorreu um erro inesperado. Informe o traceId ao suporte.");
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }
}

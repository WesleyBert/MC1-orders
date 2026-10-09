using System.Net;
using Microsoft.AspNetCore.Hosting;
using static Orders.IntegrationTests.HttpExtensions;

namespace Orders.IntegrationTests;

public sealed class SpaFactory : OrdersApiFactory
{
    public const string IndexMarker = "<div id=\"root\"></div>";

    private readonly string _webRoot = Directory.CreateTempSubdirectory("orders-spa-").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), $"<!doctype html><html><body>{IndexMarker}</body></html>");
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets"));
        File.WriteAllText(Path.Combine(_webRoot, "assets", "app.js"), "console.log('ok')");
        builder.UseWebRoot(_webRoot);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }
}

public sealed class SpaHostingTests(SpaFactory factory) : IClassFixture<SpaFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/")]
    [InlineData("/pedidos")]
    [InlineData("/pedidos/123?status=Paid")]
    public async Task NonApiRoutes_ServeTheSpa(string path)
    {
        var response = await _client.GetAsync(Url(path));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        (await response.Content.ReadAsStringAsync()).Should().Contain(SpaFactory.IndexMarker);
    }

    [Fact]
    public async Task StaticAssets_AreServed()
    {
        var response = await _client.GetAsync(Url("/assets/app.js"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MissingAsset_IsNotReplacedByIndex()
    {
        (await _client.GetAsync(Url("/assets/missing.js"))).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1/unknown")]
    [InlineData("/API/v1/orders/not-a-guid")]
    public async Task ApiRoutes_NeverFallBackToSpa(string path)
    {
        var response = await _client.GetAsync(Url(path));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ApiMethodNotAllowed_StillReturns405WithSpaHosted()
    {
        var response = await _client.PatchAsync(Url("/api/v1/orders"), RawJson("{}"));

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}

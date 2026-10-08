using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Orders.IntegrationTests;

public sealed class OpenApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static Orders.IntegrationTests.HttpExtensions;

namespace Orders.IntegrationTests;

public sealed class OrdersEndpointsTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private const string Orders = "/api/v1/orders";

    private readonly HttpClient _client = factory.CreateClient();

    private static object ValidOrder(string customer = "Maria Souza") =>
        new { customerName = customer, description = "Reposição de gôndola", totalAmount = 1520.50m };

    private static object UpdateBody(string status = "Open", decimal amount = 1720m) =>
        new { customerName = "Maria Souza", description = "Reposição de gôndola", totalAmount = amount, status };

    private async Task<JsonElement> CreateAsync(string customer = "Maria Souza")
    {
        var response = await _client.PostAsJsonAsync(Url(Orders), ValidOrder(customer));
        response.EnsureSuccessStatusCode();
        return await response.JsonAsync();
    }

    private static string IdOf(JsonElement order) => order.GetProperty("id").GetString()!;

    [Fact]
    public async Task List_Defaults_ReturnFirstPageWithTotals()
    {
        var response = await _client.GetAsync(Url(Orders));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var body = await response.JsonAsync();
        body.GetProperty("page").GetInt32().Should().Be(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(20);
        body.GetProperty("items").GetArrayLength().Should().Be(20);
        body.GetProperty("totalItems").GetInt32().Should().BeGreaterThanOrEqualTo(OrdersApiFactory.SeedCount);
        body.GetProperty("items")[0].GetProperty("status").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task List_SearchIgnoresAccents()
    {
        await CreateAsync("Conceição Integração");

        var body = await (await _client.GetAsync(Url($"{Orders}?search=conceicao integracao"))).JsonAsync();

        body.GetProperty("items").EnumerateArray()
            .Should().Contain(o => o.GetProperty("customerName").GetString() == "Conceição Integração");
    }

    [Fact]
    public async Task List_StatusFilter_ReturnsOnlyThatStatus()
    {
        var body = await (await _client.GetAsync(Url($"{Orders}?status=paid&pageSize=100"))).JsonAsync();

        body.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(o => o.GetProperty("status").GetString() == "Paid");
    }

    [Theory]
    [InlineData("pageSize=500", "pageSize", "O tamanho da página deve estar entre 1 e 100.")]
    [InlineData("page=0", "page", "A página deve ser maior ou igual a 1.")]
    [InlineData("status=Shipped", "status", "Status inválido. Valores aceitos: Open, Paid, Cancelled.")]
    [InlineData("page=abc", "page", "O valor informado é inválido.")]
    public async Task List_InvalidQuery_ReturnsValidationProblem(string query, string field, string message)
    {
        var response = await _client.GetAsync(Url($"{Orders}?{query}"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.JsonAsync();
        problem.Code().Should().Be("request.validation");
        problem.GetProperty("title").GetString().Should().Be("Um ou mais campos são inválidos.");
        problem.GetProperty("errors").GetProperty(field)[0].GetString().Should().Be(message);
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_Valid_Returns201WithLocationAndETag()
    {
        var response = await _client.PostAsJsonAsync(Url(Orders), new
        {
            customerName = " Maria Souza ",
            description = "Reposição de gôndola",
            totalAmount = 1520.50m,
            status = "Paid",
            id = Guid.Empty,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.JsonAsync();
        response.Headers.Location!.ToString().Should().EndWith($"{Orders}/{IdOf(order)}");
        response.Headers.ETag!.Tag.Should().Be("\"1\"");
        order.GetProperty("status").GetString().Should().Be("Open");
        order.GetProperty("customerName").GetString().Should().Be("Maria Souza");
        order.GetProperty("version").GetInt64().Should().Be(1);
        IdOf(order).Should().NotBe(Guid.Empty.ToString());
    }

    [Fact]
    public async Task Create_Invalid_ReturnsFieldErrorsInCamelCase()
    {
        var response = await _client.PostAsJsonAsync(Url(Orders), new { customerName = "", description = "ok ok", totalAmount = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.JsonAsync()).GetProperty("errors");
        errors.GetProperty("customerName")[0].GetString().Should().Be("O nome do cliente é obrigatório.");
        errors.GetProperty("totalAmount")[0].GetString().Should().Be("O valor total deve ser maior que zero.");
    }

    [Theory]
    [InlineData("{ invalid json")]
    [InlineData("{\"customerName\": \"Maria\", \"description\": \"Pedido\", \"totalAmount\": \"abc\"}")]
    [InlineData("")]
    public async Task Create_MalformedBody_ReturnsMalformedJson(string json)
    {
        var response = await _client.PostAsync(Url(Orders), RawJson(json));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.JsonAsync();
        problem.Code().Should().Be("request.malformed_json");
        problem.GetProperty("detail").GetString().Should().Be("O corpo da requisição não é um JSON válido.");
    }

    [Fact]
    public async Task Create_WrongContentType_Returns415()
    {
        var response = await _client.PostAsync(Url(Orders), new StringContent("customerName=Maria"));

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        (await response.JsonAsync()).Code().Should().Be("request.unsupported_media_type");
    }

    [Fact]
    public async Task Get_Existing_ReturnsOrderWithETag()
    {
        var created = await CreateAsync();

        var response = await _client.GetAsync(Url($"{Orders}/{IdOf(created)}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag!.Tag.Should().Be("\"1\"");
        (await response.JsonAsync()).GetProperty("number").GetInt64().Should().BePositive();
    }

    [Fact]
    public async Task Get_Missing_Returns404Problem()
    {
        var response = await _client.GetAsync(Url($"{Orders}/{Guid.NewGuid()}"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.JsonAsync();
        problem.Code().Should().Be("order.not_found");
        problem.GetProperty("detail").GetString().Should().Be("Pedido não encontrado.");
        problem.GetProperty("title").GetString().Should().Be("Não encontrado");
    }

    [Fact]
    public async Task Get_InvalidId_Returns404ProblemNotSpa()
    {
        var response = await _client.GetAsync(Url($"{Orders}/not-a-guid"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Update_WithMatchingIfMatch_Returns200AndNewETag()
    {
        var created = await CreateAsync();

        var response = await _client.SendAsync(HttpMethod.Put, $"{Orders}/{IdOf(created)}", UpdateBody("Paid"), "\"1\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag!.Tag.Should().Be("\"2\"");
        var order = await response.JsonAsync();
        order.GetProperty("status").GetString().Should().Be("Paid");
        order.GetProperty("totalAmount").GetDecimal().Should().Be(1720m);
    }

    [Fact]
    public async Task Update_WithStaleIfMatch_Returns412()
    {
        var created = await CreateAsync();
        await _client.SendAsync(HttpMethod.Put, $"{Orders}/{IdOf(created)}", UpdateBody(), "\"1\"");

        var response = await _client.SendAsync(HttpMethod.Put, $"{Orders}/{IdOf(created)}", UpdateBody("Paid"), "\"1\"");

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var problem = await response.JsonAsync();
        problem.Code().Should().Be("order.concurrency_conflict");
        problem.GetProperty("detail").GetString().Should().StartWith("Este pedido foi alterado por outra pessoa.");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("\"0\"")]
    [InlineData("\"x\"")]
    public async Task Update_InvalidIfMatch_Returns400(string ifMatch)
    {
        var created = await CreateAsync();

        var response = await _client.SendAsync(HttpMethod.Put, $"{Orders}/{IdOf(created)}", UpdateBody(), ifMatch);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.JsonAsync()).GetProperty("errors").TryGetProperty("If-Match", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Update_WeakETagAndWildcard_AreAccepted()
    {
        var created = await CreateAsync();

        (await _client.SendAsync(HttpMethod.Put, $"{Orders}/{IdOf(created)}", UpdateBody(), "W/\"1\""))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.SendAsync(HttpMethod.Put, $"{Orders}/{IdOf(created)}", UpdateBody(), "*"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_PaidOrder_Returns409ImmutableState()
    {
        var created = await CreateAsync();
        await _client.SendAsync(HttpMethod.Put, $"{Orders}/{IdOf(created)}", UpdateBody("Paid"));

        var response = await _client.SendAsync(HttpMethod.Put, $"{Orders}/{IdOf(created)}", UpdateBody("Open"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.JsonAsync();
        problem.Code().Should().Be("order.immutable_state");
        problem.GetProperty("detail").GetString().Should().Be("Pedidos com status Pago não podem ser alterados.");
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        var response = await _client.SendAsync(HttpMethod.Put, $"{Orders}/{Guid.NewGuid()}", UpdateBody());

        (await response.JsonAsync()).Code().Should().Be("order.not_found");
    }

    [Fact]
    public async Task Delete_OpenOrder_Returns204ThenGetReturns404()
    {
        var created = await CreateAsync();

        var response = await _client.SendAsync(HttpMethod.Delete, $"{Orders}/{IdOf(created)}", ifMatch: "\"1\"");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync(Url($"{Orders}/{IdOf(created)}"))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.DeleteAsync(Url($"{Orders}/{IdOf(created)}"))).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_PaidOrder_Returns409()
    {
        var created = await CreateAsync();
        await _client.SendAsync(HttpMethod.Put, $"{Orders}/{IdOf(created)}", UpdateBody("Paid"));

        var response = await _client.DeleteAsync(Url($"{Orders}/{IdOf(created)}"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.JsonAsync()).Code().Should().Be("order.delete_not_allowed");
    }

    [Fact]
    public async Task Delete_StaleIfMatch_Returns412()
    {
        var created = await CreateAsync();

        var response = await _client.SendAsync(HttpMethod.Delete, $"{Orders}/{IdOf(created)}", ifMatch: "\"5\"");

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }
}

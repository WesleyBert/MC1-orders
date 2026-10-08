using Orders.Application.Orders;

namespace Orders.UnitTests.Application;

public sealed class ValidatorTests
{
    private readonly CreateOrderRequestValidator _create = new();
    private readonly UpdateOrderRequestValidator _update = new();
    private readonly ListOrdersQueryValidator _list = new();

    [Fact]
    public void Create_ValidRequest_Passes()
    {
        _create.Validate(new CreateOrderRequest("Maria Souza", "Reposição de gôndola", 1520.50m))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyRequest_ReportsEveryRequiredField()
    {
        var result = _create.Validate(new CreateOrderRequest(null, null, null));

        result.Errors.Should().BeEquivalentTo(new[]
        {
            new { PropertyName = "CustomerName", ErrorMessage = "O nome do cliente é obrigatório." },
            new { PropertyName = "Description", ErrorMessage = "A descrição é obrigatória." },
            new { PropertyName = "TotalAmount", ErrorMessage = "O valor total é obrigatório." },
        });
    }

    [Fact]
    public void Create_MultipleInvalidFields_ReportsAllAtOnce()
    {
        var result = _create.Validate(new CreateOrderRequest("A", "", 10.999m));

        result.Errors.Select(e => e.ErrorMessage).Should().BeEquivalentTo(
            "O nome do cliente deve ter entre 2 e 150 caracteres.",
            "A descrição é obrigatória.",
            "O valor total deve ter no máximo 2 casas decimais.");
    }

    [Theory]
    [InlineData("   ", "O nome do cliente é obrigatório.")]
    [InlineData(" A ", "O nome do cliente deve ter entre 2 e 150 caracteres.")]
    public void Create_CustomerName_IsTrimmedBeforeValidation(string name, string expected)
    {
        _create.Validate(new CreateOrderRequest(name, "Descrição válida", 10m))
            .Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(expected);
    }

    [Fact]
    public void Create_CustomerNameAtMaxLength_Passes()
    {
        _create.Validate(new CreateOrderRequest(new string('a', 150), "Descrição válida", 10m))
            .IsValid.Should().BeTrue();
        _create.Validate(new CreateOrderRequest(new string('a', 151), "Descrição válida", 10m))
            .IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, "O valor total deve ser maior que zero.")]
    [InlineData(-5, "O valor total deve ser maior que zero.")]
    [InlineData(1_000_000_000, "O valor total deve ser no máximo R$ 999.999.999,99.")]
    [InlineData(10.001, "O valor total deve ter no máximo 2 casas decimais.")]
    public void Create_InvalidTotalAmount_ReturnsCatalogMessage(decimal amount, string expected)
    {
        _create.Validate(new CreateOrderRequest("Maria", "Descrição válida", amount))
            .Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(expected);
    }

    [Fact]
    public void Create_TrailingZeroDecimals_Passes()
    {
        _create.Validate(new CreateOrderRequest("Maria", "Descrição válida", 10.500m)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Open")]
    [InlineData("paid")]
    [InlineData("CANCELLED")]
    public void Update_KnownStatusNames_AreAcceptedCaseInsensitive(string status)
    {
        _update.Validate(new UpdateOrderRequest("Maria", "Descrição válida", 10m, status)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "O status é obrigatório.")]
    [InlineData("", "O status é obrigatório.")]
    [InlineData("Shipped", "Status inválido. Valores aceitos: Open, Paid, Cancelled.")]
    [InlineData("2", "Status inválido. Valores aceitos: Open, Paid, Cancelled.")]
    [InlineData("Open,Paid", "Status inválido. Valores aceitos: Open, Paid, Cancelled.")]
    public void Update_InvalidStatus_ReturnsCatalogMessage(string? status, string expected)
    {
        _update.Validate(new UpdateOrderRequest("Maria", "Descrição válida", 10m, status))
            .Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(expected);
    }

    [Fact]
    public void List_Defaults_AreValid()
    {
        _list.Validate(new ListOrdersQuery()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 20, "A página deve ser maior ou igual a 1.")]
    [InlineData(1, 0, "O tamanho da página deve estar entre 1 e 100.")]
    [InlineData(1, 101, "O tamanho da página deve estar entre 1 e 100.")]
    public void List_InvalidPaging_ReturnsCatalogMessage(int page, int pageSize, string expected)
    {
        _list.Validate(new ListOrdersQuery { Page = page, PageSize = pageSize })
            .Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(expected);
    }

    [Fact]
    public void List_InvalidFilters_ReportEachField()
    {
        var result = _list.Validate(new ListOrdersQuery
        {
            Search = new string('x', 101),
            Status = "Shipped",
            SortBy = "price",
            SortDir = "up",
        });

        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo("Search", "Status", "SortBy", "SortDir");
    }

    [Fact]
    public void List_ValidFilters_Pass()
    {
        _list.Validate(new ListOrdersQuery { Search = "joão", Status = "paid", SortBy = "totalAmount", SortDir = "ASC" })
            .IsValid.Should().BeTrue();
    }
}

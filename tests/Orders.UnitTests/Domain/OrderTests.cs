using Orders.Domain.Common;
using Orders.Domain.Entities;
using Orders.Domain.Enums;

namespace Orders.UnitTests.Domain;

public sealed class OrderTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = CreatedAt.AddHours(2);

    private static Order NewOrder() =>
        Order.Create(10001, "Maria Souza", "Reposição de gôndola", 1520.50m, CreatedAt);

    private static Order InStatus(OrderStatus status)
    {
        var order = NewOrder();
        if (status != OrderStatus.Open)
        {
            order.Update("Maria Souza", "Reposição de gôndola", 1520.50m, status, Later);
        }

        return order;
    }

    [Fact]
    public void Create_StartsOpenWithVersionOneAndServerFields()
    {
        var order = Order.Create(10001, "  Maria Souza  ", "  Reposição de gôndola ", 1520.50m, CreatedAt);

        order.Status.Should().Be(OrderStatus.Open);
        order.Version.Should().Be(1);
        order.Number.Should().Be(10001);
        order.Id.Version.Should().Be(7);
        order.CreatedAt.Should().Be(CreatedAt);
        order.UpdatedAt.Should().Be(CreatedAt);
        order.CustomerName.Should().Be("Maria Souza");
        order.Description.Should().Be("Reposição de gôndola");
    }

    [Fact]
    public void Update_OpenOrder_ChangesFieldsAndIncrementsVersion()
    {
        var order = NewOrder();

        var result = order.Update("João Conceição", "Pedido mensal", 99.90m, OrderStatus.Open, Later);

        result.IsSuccess.Should().BeTrue();
        order.Should().BeEquivalentTo(new
        {
            Number = 10001L,
            CustomerName = "João Conceição",
            Description = "Pedido mensal",
            TotalAmount = 99.90m,
            Status = OrderStatus.Open,
            CreatedAt,
            UpdatedAt = Later,
            Version = 2L,
        });
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Cancelled)]
    public void Update_OpenOrder_CanMoveToFinalStatus(OrderStatus target)
    {
        var order = NewOrder();

        var result = order.Update("Maria Souza", "Reposição de gôndola", 1520.50m, target, Later);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(target);
        order.Version.Should().Be(2);
        order.IsFinal.Should().BeTrue();
    }

    [Theory]
    [InlineData(OrderStatus.Paid, OrderStatus.Open, "Pago")]
    [InlineData(OrderStatus.Paid, OrderStatus.Paid, "Pago")]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled, "Pago")]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Open, "Cancelado")]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, "Cancelado")]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Cancelled, "Cancelado")]
    public void Update_FinalOrder_FailsWithImmutableStateAndKeepsState(
        OrderStatus current, OrderStatus target, string label)
    {
        var order = InStatus(current);

        var result = order.Update("Outro Cliente", "Outra descrição", 1m, target, Later.AddHours(1));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("order.immutable_state");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Be($"Pedidos com status {label} não podem ser alterados.");
        order.Should().BeEquivalentTo(new
        {
            Status = current,
            CustomerName = "Maria Souza",
            TotalAmount = 1520.50m,
            UpdatedAt = Later,
            Version = 2L,
        });
    }

    [Fact]
    public void Update_InvalidInput_ThrowsAndLeavesOrderUntouched()
    {
        var order = NewOrder();

        var act = () => order.Update("Novo nome", "Nova descrição", 0m, OrderStatus.Paid, Later);

        act.Should().Throw<ArgumentOutOfRangeException>();
        order.Should().BeEquivalentTo(new
        {
            CustomerName = "Maria Souza",
            Status = OrderStatus.Open,
            Version = 1L,
        });
    }

    [Theory]
    [InlineData(OrderStatus.Open, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Paid, false)]
    public void EnsureCanDelete_BlocksOnlyPaid(OrderStatus status, bool allowed)
    {
        var result = InStatus(status).EnsureCanDelete();

        result.IsSuccess.Should().Be(allowed);
        if (!allowed)
        {
            result.Error!.Code.Should().Be("order.delete_not_allowed");
        }
    }

    [Fact]
    public void Restore_RebuildsOrderWithoutChangingState()
    {
        var id = Guid.CreateVersion7(CreatedAt);

        var order = Order.Restore(id, 10500, "Ana", "Pedido", 10m, OrderStatus.Paid, CreatedAt, Later, 4);

        order.Should().BeEquivalentTo(new
        {
            Id = id,
            Number = 10500L,
            Status = OrderStatus.Paid,
            UpdatedAt = Later,
            Version = 4L,
        });
    }

    [Fact]
    public void Equality_IsBasedOnIdentity()
    {
        var order = NewOrder();
        var sameIdentity = Order.Restore(
            order.Id, order.Number, "Outro", "Outra descrição", 1m, OrderStatus.Paid, CreatedAt, Later, 9);

        sameIdentity.Should().Be(order);
        (sameIdentity == order).Should().BeTrue();
        NewOrder().Should().NotBe(order);
    }

    [Fact]
    public void Value_OnFailure_Throws()
    {
        Result<int> result = InStatus(OrderStatus.Paid).EnsureCanDelete().Error!;

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("A", "Descrição válida", 10)]
    [InlineData("   ", "Descrição válida", 10)]
    [InlineData("Maria", "ab", 10)]
    [InlineData("Maria", "Descrição válida", 0)]
    [InlineData("Maria", "Descrição válida", -1)]
    [InlineData("Maria", "Descrição válida", 10.999)]
    public void Create_InvalidInput_ThrowsArgumentException(string customer, string description, decimal amount)
    {
        var act = () => Order.Create(10001, customer, description, amount, CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_AcceptsTrailingZeroDecimals()
    {
        Order.Create(10001, "Maria", "Descrição válida", 10.500m, CreatedAt).TotalAmount.Should().Be(10.5m);
    }

    [Fact]
    public void Update_UndefinedStatus_Throws()
    {
        var act = () => NewOrder().Update("Maria", "Descrição válida", 10m, (OrderStatus)99, Later);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

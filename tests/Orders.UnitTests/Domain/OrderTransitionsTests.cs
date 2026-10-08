using Orders.Domain.Orders;

namespace Orders.UnitTests.Domain;

public sealed class OrderTransitionsTests
{
    // Matriz De → Para completa (docs/02, seção 4).
    [Theory]
    [InlineData(OrderStatus.Open, OrderStatus.Open, true)]
    [InlineData(OrderStatus.Open, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Open, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Open, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Open, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Cancelled, false)]
    public void CanTransition_FollowsStateMatrix(OrderStatus from, OrderStatus to, bool expected)
    {
        OrderTransitions.CanTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData(OrderStatus.Open, false)]
    [InlineData(OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    public void IsFinal_OnlyForPaidAndCancelled(OrderStatus status, bool expected)
    {
        OrderTransitions.IsFinal(status).Should().Be(expected);
    }

    [Theory]
    [InlineData(OrderStatus.Open, true)]
    [InlineData(OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Cancelled, true)]
    public void CanDelete_BlocksOnlyPaid(OrderStatus status, bool expected)
    {
        OrderTransitions.CanDelete(status).Should().Be(expected);
    }

    [Fact]
    public void EveryStatus_IsMappedInTransitionTable()
    {
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            var act = () => OrderTransitions.AllowedTargets(status);
            act.Should().NotThrow($"status {status} precisa estar na tabela de transições");
            OrderTransitions.DisplayName(status).Should().NotBeNullOrWhiteSpace();
        }
    }
}

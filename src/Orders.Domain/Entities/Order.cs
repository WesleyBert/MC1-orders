using Orders.Domain.Common;
using Orders.Domain.Enums;
using Orders.Domain.Errors;
using Orders.Domain.Rules;

namespace Orders.Domain.Entities;

public sealed class Order : Entity<Guid>
{
    private Order(
        Guid id,
        long number,
        string customerName,
        string description,
        decimal totalAmount,
        OrderStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        long version)
        : base(id)
    {
        Number = number;
        CustomerName = customerName;
        Description = description;
        TotalAmount = totalAmount;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Version = version;
    }

    public long Number { get; }

    public string CustomerName { get; private set; }

    public string Description { get; private set; }

    public decimal TotalAmount { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public long Version { get; private set; }

    public bool IsFinal => OrderTransitions.IsFinal(Status);

    public static Order Create(
        long number, string customerName, string description, decimal totalAmount, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);

        return new Order(
            Guid.CreateVersion7(now),
            number,
            GuardCustomerName(customerName),
            GuardDescription(description),
            GuardTotalAmount(totalAmount),
            OrderStatus.Open,
            now,
            now,
            version: 1);
    }

    public static Order Restore(
        Guid id,
        long number,
        string customerName,
        string description,
        decimal totalAmount,
        OrderStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        long version) =>
        new(id, number, customerName, description, totalAmount, status, createdAt, updatedAt, version);

    public Result Update(
        string customerName, string description, decimal totalAmount, OrderStatus status, DateTimeOffset now)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        if (!OrderTransitions.CanTransition(Status, status))
        {
            return OrderErrors.ImmutableState(Status);
        }

        var name = GuardCustomerName(customerName);
        var text = GuardDescription(description);
        var amount = GuardTotalAmount(totalAmount);

        CustomerName = name;
        Description = text;
        TotalAmount = amount;
        Status = status;
        UpdatedAt = now;
        Version++;

        return Result.Success();
    }

    public Result EnsureCanDelete() =>
        OrderTransitions.CanDelete(Status) ? Result.Success() : OrderErrors.DeleteNotAllowed;

    private static string GuardCustomerName(string customerName) =>
        GuardText(customerName, OrderRules.CustomerNameMinLength, OrderRules.CustomerNameMaxLength, nameof(customerName));

    private static string GuardDescription(string description) =>
        GuardText(description, OrderRules.DescriptionMinLength, OrderRules.DescriptionMaxLength, nameof(description));

    private static string GuardText(string value, int min, int max, string paramName)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        var trimmed = value.Trim();

        if (trimmed.Length < min || trimmed.Length > max)
        {
            throw new ArgumentOutOfRangeException(paramName, trimmed.Length, $"Deve ter entre {min} e {max} caracteres.");
        }

        return trimmed;
    }

    private static decimal GuardTotalAmount(decimal totalAmount)
    {
        if (totalAmount <= 0 || totalAmount > OrderRules.TotalAmountMax ||
            decimal.Round(totalAmount, OrderRules.TotalAmountMaxDecimals) != totalAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), totalAmount, "Valor total inválido.");
        }

        return totalAmount;
    }
}

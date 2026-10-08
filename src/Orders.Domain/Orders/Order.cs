using Orders.Domain.Common;

namespace Orders.Domain.Orders;

/// <summary>
/// Pedido imutável. Toda alteração gera uma nova instância com <see cref="Version"/> incrementada,
/// o que permite compare-and-swap seguro no repositório em memória.
/// </summary>
public sealed record Order
{
    private Order() { }

    public Guid Id { get; private init; }

    /// <summary>Número sequencial legível (ex.: #10234), atribuído pelo repositório.</summary>
    public long Number { get; private init; }

    public string CustomerName { get; private init; } = string.Empty;

    public string Description { get; private init; } = string.Empty;

    public decimal TotalAmount { get; private init; }

    public OrderStatus Status { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset UpdatedAt { get; private init; }

    /// <summary>Versão para concorrência otimista; começa em 1 e cresce a cada alteração.</summary>
    public long Version { get; private init; }

    public bool IsFinal => OrderTransitions.IsFinal(Status);

    /// <summary>
    /// Cria um pedido em <see cref="OrderStatus.Open"/>. A entrada já deve ter passado pelos
    /// validadores da aplicação; aqui as regras são reforçadas como invariantes.
    /// </summary>
    public static Order Create(
        long number, string customerName, string description, decimal totalAmount, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);

        return new Order
        {
            Id = Guid.CreateVersion7(now),
            Number = number,
            CustomerName = GuardCustomerName(customerName),
            Description = GuardDescription(description),
            TotalAmount = GuardTotalAmount(totalAmount),
            Status = OrderStatus.Open,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };
    }

    /// <summary>
    /// Aplica edição de campos e, opcionalmente, transição de status em uma única operação.
    /// Só é permitido enquanto o pedido está em <see cref="OrderStatus.Open"/>.
    /// </summary>
    public Result<Order> Update(
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

        return this with
        {
            CustomerName = GuardCustomerName(customerName),
            Description = GuardDescription(description),
            TotalAmount = GuardTotalAmount(totalAmount),
            Status = status,
            UpdatedAt = now,
            Version = Version + 1,
        };
    }

    public Result EnsureCanDelete() =>
        OrderTransitions.CanDelete(Status) ? Result.Success() : OrderErrors.DeleteNotAllowed;

    private static string GuardCustomerName(string value) =>
        GuardText(value, OrderRules.CustomerNameMinLength, OrderRules.CustomerNameMaxLength, nameof(CustomerName));

    private static string GuardDescription(string value) =>
        GuardText(value, OrderRules.DescriptionMinLength, OrderRules.DescriptionMaxLength, nameof(Description));

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

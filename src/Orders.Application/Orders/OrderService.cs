using FluentValidation;
using Microsoft.Extensions.Logging;
using Orders.Application.Common;
using Orders.Application.Orders.Contracts;
using Orders.Application.Orders.Persistence;
using Orders.Domain.Common;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Orders.Domain.Errors;

namespace Orders.Application.Orders;

public sealed partial class OrderService(
    IOrderRepository repository,
    IValidator<CreateOrderRequest> createValidator,
    IValidator<UpdateOrderRequest> updateValidator,
    IValidator<ListOrdersQuery> listValidator,
    TimeProvider timeProvider,
    ILogger<OrderService> logger)
{
    internal const int MaxWriteAttempts = 3;

    public async Task<Result<OrderResponse>> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, request, ct) is { } invalid)
        {
            return invalid;
        }

        var order = Order.Create(
            repository.NextNumber(),
            request.CustomerName!,
            request.Description!,
            request.TotalAmount!.Value,
            timeProvider.GetUtcNow());

        await repository.AddAsync(order, ct);
        LogOrderCreated(order.Id, order.Number);

        return OrderResponse.From(order);
    }

    public async Task<Result<OrderResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var order = await repository.GetAsync(id, ct);
        return order is null ? OrderErrors.NotFound : OrderResponse.From(order);
    }

    public async Task<Result<PagedResponse<OrderResponse>>> ListAsync(ListOrdersQuery query, CancellationToken ct)
    {
        if (await ValidateAsync(listValidator, query, ct) is { } invalid)
        {
            return invalid;
        }

        var criteria = new OrderListCriteria(
            Search: string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            Status: OrderParsing.ParseStatus(query.Status),
            query.Page,
            query.PageSize,
            SortBy: OrderParsing.ParseSortField(query.SortBy) ?? OrderSortField.CreatedAt,
            Descending: OrderParsing.ParseSortDescending(query.SortDir) ?? true);

        var page = await repository.ListAsync(criteria, ct);

        return new PagedResponse<OrderResponse>(
            page.Items.Select(OrderResponse.From).ToList(), query.Page, query.PageSize, page.TotalItems);
    }

    public async Task<Result<OrderResponse>> UpdateAsync(
        Guid id, UpdateOrderRequest request, long? expectedVersion, CancellationToken ct)
    {
        if (await ValidateAsync(updateValidator, request, ct) is { } invalid)
        {
            return invalid;
        }

        var status = OrderParsing.ParseStatus(request.Status)!.Value;

        for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
        {
            var order = await repository.GetAsync(id, ct);
            if (order is null)
            {
                return OrderErrors.NotFound;
            }

            if (expectedVersion is { } expected && expected != order.Version)
            {
                LogVersionMismatch(id, expected, order.Version);
                return OrderErrors.VersionMismatch;
            }

            var loadedVersion = order.Version;
            var updated = order.Update(
                request.CustomerName!,
                request.Description!,
                request.TotalAmount!.Value,
                status,
                timeProvider.GetUtcNow());

            if (!updated.IsSuccess)
            {
                return updated.Error;
            }

            if (await repository.TryUpdateAsync(order, loadedVersion, ct))
            {
                LogOrderUpdated(id, order.Status, order.Version);
                return OrderResponse.From(order);
            }
        }

        LogWriteContention(id, MaxWriteAttempts);
        return OrderErrors.ConcurrencyConflict;
    }

    public async Task<Result> DeleteAsync(Guid id, long? expectedVersion, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
        {
            var order = await repository.GetAsync(id, ct);
            if (order is null)
            {
                return OrderErrors.NotFound;
            }

            if (expectedVersion is { } expected && expected != order.Version)
            {
                LogVersionMismatch(id, expected, order.Version);
                return OrderErrors.VersionMismatch;
            }

            var canDelete = order.EnsureCanDelete();
            if (!canDelete.IsSuccess)
            {
                return canDelete;
            }

            if (await repository.TryRemoveAsync(id, order.Version, ct))
            {
                LogOrderDeleted(id);
                return Result.Success();
            }
        }

        LogWriteContention(id, MaxWriteAttempts);
        return OrderErrors.ConcurrencyConflict;
    }

    private static async Task<Error?> ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (result.IsValid)
        {
            return null;
        }

        var fields = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return Error.Validation(fields);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pedido {OrderId} (#{Number}) criado")]
    private partial void LogOrderCreated(Guid orderId, long number);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pedido {OrderId} atualizado: status {Status}, versão {Version}")]
    private partial void LogOrderUpdated(Guid orderId, OrderStatus status, long version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pedido {OrderId} excluído")]
    private partial void LogOrderDeleted(Guid orderId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Conflito de versão no pedido {OrderId}: esperada {ExpectedVersion}, atual {CurrentVersion}")]
    private partial void LogVersionMismatch(Guid orderId, long expectedVersion, long currentVersion);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pedido {OrderId} sob contenção: gravação falhou {Attempts} vezes")]
    private partial void LogWriteContention(Guid orderId, int attempts);
}

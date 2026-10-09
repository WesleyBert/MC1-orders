using Microsoft.AspNetCore.Mvc;
using Orders.Api.Errors;
using Orders.Api.Http;
using Orders.Application.Common;
using Orders.Application.Orders;
using Orders.Application.Orders.Contracts;
using Orders.Domain.Common;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(OrderService orderService) : ControllerBase
{
    private const string GetOrderRoute = "GetOrder";

    [HttpGet]
    [EndpointSummary("Lista pedidos com busca, filtro por status, ordenação e paginação")]
    [ProducesResponseType<PagedResponse<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ResultExtensions.ProblemJson)]
    public async Task<IActionResult> List([FromQuery] ListOrdersQuery query, CancellationToken ct)
    {
        var result = await orderService.ListAsync(query, ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    [HttpGet("summary")]
    [EndpointSummary("Conta os pedidos por status em uma única chamada")]
    [ProducesResponseType<OrdersSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken ct) =>
        Ok(await orderService.GetSummaryAsync(ct));

    [HttpGet("{id:guid}", Name = GetOrderRoute)]
    [EndpointSummary("Obtém um pedido pelo identificador")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ResultExtensions.ProblemJson)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await orderService.GetAsync(id, ct);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        Response.Headers.ETag = ETag.Format(result.Value.Version);
        return Ok(result.Value);
    }

    [HttpPost]
    [Consumes("application/json")]
    [EndpointSummary("Cria um pedido com status Open")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ResultExtensions.ProblemJson)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orderService.CreateAsync(request, ct);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        Response.Headers.ETag = ETag.Format(result.Value.Version);
        return CreatedAtRoute(GetOrderRoute, new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [EndpointSummary("Atualiza campos e status de um pedido aberto")]
    [EndpointDescription("Envie If-Match com o ETag lido para evitar sobrescrever alterações de outra pessoa.")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ResultExtensions.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ResultExtensions.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ResultExtensions.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed, ResultExtensions.ProblemJson)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateOrderRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!ETag.TryParseIfMatch(ifMatch, out var expectedVersion))
        {
            return this.ToProblem(InvalidIfMatch());
        }

        var result = await orderService.UpdateAsync(id, request, expectedVersion, ct);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        Response.Headers.ETag = ETag.Format(result.Value.Version);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Exclui um pedido aberto ou cancelado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ResultExtensions.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ResultExtensions.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ResultExtensions.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed, ResultExtensions.ProblemJson)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!ETag.TryParseIfMatch(ifMatch, out var expectedVersion))
        {
            return this.ToProblem(InvalidIfMatch());
        }

        var result = await orderService.DeleteAsync(id, expectedVersion, ct);
        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    private static Error InvalidIfMatch() => Error.Validation(new Dictionary<string, string[]>
    {
        ["If-Match"] = ["Cabeçalho If-Match inválido. Use o valor do ETag, por exemplo \"3\"."],
    });
}

using Orders.Api.Configuration;

namespace Orders.Api.Middleware;

public sealed class RequestSizeLimitMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > ApiOptions.MaxRequestBodyBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return Task.CompletedTask;
        }

        return next(context);
    }
}

using System.Diagnostics;

namespace Orders.Api.Middleware;

public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
                LogRequest(context.Request.Method, path, context.Response.StatusCode, elapsedMs, traceId);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Method} {Path} respondeu {StatusCode} em {ElapsedMs:0.0} ms (trace {TraceId})")]
    private partial void LogRequest(string method, string path, int statusCode, double elapsedMs, string traceId);
}

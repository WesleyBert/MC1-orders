using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Orders.Api.Configuration;
using Orders.Api.Health;
using Orders.Api.Middleware;
using Scalar.AspNetCore;

namespace Orders.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestSizeLimitMiddleware>();
        app.UseStaticFiles();

        app.MapControllers();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(SeedReadinessHealthCheck.Tag),
        });

        if (app.Services.GetRequiredService<IOptions<ApiOptions>>().Value.EnableDocs)
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html");

        return app;
    }
}

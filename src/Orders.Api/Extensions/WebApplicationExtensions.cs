using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Orders.Api.Configuration;
using Orders.Api.Health;
using Orders.Api.Http;
using Orders.Api.Middleware;

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
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "MC1 Orders API v1");
                options.DocumentTitle = "MC1 Orders API";
            });
        }

        app.MapFallbackToFile($"{{*path:{NotApiRouteConstraint.Name}:nonfile}}", "index.html");

        return app;
    }
}

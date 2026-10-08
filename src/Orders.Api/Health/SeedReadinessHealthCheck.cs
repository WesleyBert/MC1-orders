using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Infrastructure.Seeding;

namespace Orders.Api.Health;

public sealed class SeedReadinessHealthCheck(SeedStatus seedStatus) : IHealthCheck
{
    public const string Tag = "ready";

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(seedStatus.IsCompleted
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Carga inicial em andamento."));
}

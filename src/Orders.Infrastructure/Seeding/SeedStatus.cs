namespace Orders.Infrastructure.Seeding;

/// <summary>Sinaliza ao health check de prontidão que a carga inicial terminou.</summary>
public sealed class SeedStatus
{
    private volatile bool _completed;

    public bool IsCompleted => _completed;

    internal void MarkCompleted() => _completed = true;
}

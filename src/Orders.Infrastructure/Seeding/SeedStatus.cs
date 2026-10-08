namespace Orders.Infrastructure.Seeding;

public sealed class SeedStatus
{
    private volatile bool _completed;

    public bool IsCompleted => _completed;

    internal void MarkCompleted() => _completed = true;
}

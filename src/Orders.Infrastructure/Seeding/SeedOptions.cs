using System.ComponentModel.DataAnnotations;

namespace Orders.Infrastructure.Seeding;

/// <summary>Configuração da carga inicial (seção <c>Seed</c>; env: <c>Seed__Count</c> etc.).</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; init; } = true;

    [Range(0, 1_000_000)]
    public int Count { get; init; } = 10_000;

    public int RandomSeed { get; init; } = 42;
}

/// <summary>Sinaliza ao health check de prontidão que a carga inicial terminou.</summary>
public sealed class SeedStatus
{
    private volatile bool _completed;

    public bool IsCompleted => _completed;

    internal void MarkCompleted() => _completed = true;
}

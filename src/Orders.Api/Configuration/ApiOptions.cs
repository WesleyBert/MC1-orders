namespace Orders.Api.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public const long MaxRequestBodyBytes = 64 * 1024;

    public bool EnableDocs { get; init; } = true;
}

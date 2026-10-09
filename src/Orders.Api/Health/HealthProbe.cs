namespace Orders.Api.Health;

public static class HealthProbe
{
    public const string Argument = "--healthcheck";

    public static async Task<int> RunAsync()
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';')[0] ?? "8080";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        try
        {
            using var response = await client.GetAsync(new Uri($"http://localhost:{port}/health/ready"));
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (HttpRequestException)
        {
            return 1;
        }
        catch (TaskCanceledException)
        {
            return 1;
        }
    }
}

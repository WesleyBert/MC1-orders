using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Orders.IntegrationTests;

internal static class HttpExtensions
{
    public static Uri Url(string path) => new(path, UriKind.Relative);

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static StringContent RawJson(string json) => new(json, Encoding.UTF8, "application/json");

    public static Task<HttpResponseMessage> SendAsync(
        this HttpClient client, HttpMethod method, string path, object? body = null, string? ifMatch = null)
    {
        var request = new HttpRequestMessage(method, Url(path));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return client.SendAsync(request);
    }

    public static string Code(this JsonElement problem) => problem.GetProperty("code").GetString()!;
}

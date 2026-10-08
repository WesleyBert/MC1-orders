using System.Globalization;

namespace Orders.Api.Http;

public static class ETag
{
    public static string Format(long version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    public static bool TryParseIfMatch(string? header, out long? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(header) || header.Trim() == "*")
        {
            return true;
        }

        var value = header.Trim();
        if (value.StartsWith("W/", StringComparison.Ordinal))
        {
            value = value[2..];
        }

        if (value.Length < 3 || value[0] != '"' || value[^1] != '"')
        {
            return false;
        }

        if (!long.TryParse(value[1..^1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < 1)
        {
            return false;
        }

        version = parsed;
        return true;
    }
}

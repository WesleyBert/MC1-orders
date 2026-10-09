namespace Orders.Api.Http;

public sealed class NotApiRouteConstraint : IRouteConstraint
{
    public const string Name = "notapi";

    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        var value = values.TryGetValue(routeKey, out var raw) ? raw?.ToString() : null;

        return value is null
            || !(value.Equals("api", StringComparison.OrdinalIgnoreCase)
                 || value.StartsWith("api/", StringComparison.OrdinalIgnoreCase));
    }
}

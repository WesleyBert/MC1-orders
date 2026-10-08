using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Application.Orders;

namespace Orders.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<OrderService>(ServiceLifetime.Singleton);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<OrderService>();
        return services;
    }
}

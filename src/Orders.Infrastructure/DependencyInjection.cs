using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Orders.Persistence;
using Orders.Infrastructure.Orders;
using Orders.Infrastructure.Seeding;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Singleton: o "banco" é o próprio processo.
        services.AddSingleton<InMemoryOrderRepository>();
        services.AddSingleton<IOrderRepository>(sp => sp.GetRequiredService<InMemoryOrderRepository>());
        services.AddSingleton<SeedStatus>();
        services.AddHostedService<OrderSeeder>();

        return services;
    }
}

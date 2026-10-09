using System.Text.Json.Serialization;
using Orders.Api.Configuration;
using Orders.Api.Errors;
using Orders.Api.Health;
using Orders.Api.Http;

namespace Orders.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiOptions>().Bind(configuration.GetSection(ApiOptions.SectionName));
        services.AddRouting(options => options.SetParameterPolicy<NotApiRouteConstraint>(NotApiRouteConstraint.Name));

        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);

        services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsCustomization.Apply);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHealthChecks()
            .AddCheck<SeedReadinessHealthCheck>("seed", tags: [SeedReadinessHealthCheck.Tag]);

        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "MC1 Orders API";
            document.Info.Version = "v1";
            document.Info.Description = "Cadastro de pedidos com concorrência otimista via ETag/If-Match.";
            return Task.CompletedTask;
        }));

        return services;
    }
}

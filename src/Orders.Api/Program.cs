using Orders.Api.Configuration;
using Orders.Api.Extensions;
using Orders.Api.Health;
using Orders.Application;
using Orders.Infrastructure;

if (args is [HealthProbe.Argument])
{
    return await HealthProbe.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = ApiOptions.MaxRequestBodyBytes);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

app.UseApi();

await app.RunAsync();

return 0;

public partial class Program;

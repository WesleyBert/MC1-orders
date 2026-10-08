using Orders.Api.Configuration;
using Orders.Api.Extensions;
using Orders.Application;
using Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = ApiOptions.MaxRequestBodyBytes);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

app.UseApi();

app.Run();

public partial class Program;

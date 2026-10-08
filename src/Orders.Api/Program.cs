var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapControllers();

app.Run();

/// <summary>Exposto para <c>WebApplicationFactory</c> nos testes de integração.</summary>
public partial class Program;

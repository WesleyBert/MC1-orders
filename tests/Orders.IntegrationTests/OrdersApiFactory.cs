using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Orders.IntegrationTests;

public class OrdersApiFactory : WebApplicationFactory<Program>
{
    public const int SeedCount = 50;

    protected virtual int Seed => SeedCount;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Seed:Count", Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
    }
}

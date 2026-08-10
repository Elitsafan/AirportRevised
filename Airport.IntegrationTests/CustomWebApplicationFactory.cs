using Airport.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Airport.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;

    public CustomWebApplicationFactory() => _mongoContainer = new MongoDbBuilder("mongo:latest").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:Key"] = "SuperSecretTestKeyThatIsAtLeast32BytesLong!",
                ["JwtSettings:ExpiryMinutes"] = "60",
                ["LoginCredentials:Username"] = "admin",
                ["LoginCredentials:Password"] = "password123"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMongoClient));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddSingleton<IMongoClient>(sp =>
            {
                var settings = MongoClientSettings.FromConnectionString(_mongoContainer.GetConnectionString());

                return new MongoClient(settings);
            });
        });
    }

    public async Task InitializeAsync() => await _mongoContainer.StartAsync();

    public new async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();

        await base.DisposeAsync();
    }
}

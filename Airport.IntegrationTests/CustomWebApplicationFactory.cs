using Airport.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Airport.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;

    public CustomWebApplicationFactory()
    {
        // Inject variables early as environment variables so Program.cs 
        // can read them instantly during CreateBuilder()
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "TestAudience");
        Environment.SetEnvironmentVariable("JwtSettings__Key", "SuperSecretTestKeyThatIsAtLeast32BytesLong!");
        Environment.SetEnvironmentVariable("JwtSettings__ExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("LoginCredentials__Username", "admin");
        Environment.SetEnvironmentVariable("LoginCredentials__Password", "password123");

        _mongoContainer = new MongoDbBuilder("mongo:latest").Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
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

    public async Task InitializeAsync() => await _mongoContainer.StartAsync();

    public new async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();

        await base.DisposeAsync();
    }
}

using Airport.Persistence.Configurations;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Airport.Persistence
{
    public sealed class SeedData
    {
        public static async Task InitializeAsync(
            IMongoClient client,
            IOptions<AirportDbConfiguration> configuration,
            CancellationToken cancellationToken = default)
        {
            if ((await (await client.ListDatabaseNamesAsync(cancellationToken))
                .ToListAsync(cancellationToken: cancellationToken))
                .Any(name => configuration.Value.DatabaseName == name))
                return;
            await new DepartureConfiguration()
                .ConfigureAsync(client, configuration);
            await new FlightConfiguration()
                .ConfigureAsync(client, configuration);
            await new LandingConfiguration()
                .ConfigureAsync(client, configuration);
            await new RouteConfiguration()
                .ConfigureAsync(client, configuration);
            await new StationConfiguration()
                .ConfigureAsync(client, configuration);
            await new TrafficLightConfiguration()
                .ConfigureAsync(client, configuration);
        }

        public static async Task DeleteAsync(
            IMongoClient client,
            IOptions<AirportDbConfiguration> configuration,
            CancellationToken cancellationToken = default)
        {
            if ((await (await client.ListDatabaseNamesAsync(cancellationToken))
                .ToListAsync(cancellationToken: cancellationToken))
                .Any(name => configuration.Value.DatabaseName == name))
                await client.DropDatabaseAsync(configuration.Value.DatabaseName, cancellationToken);
        }
    }
}
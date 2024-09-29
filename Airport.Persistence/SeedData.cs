using MongoDB.Driver;
using Airport.Contracts.Database;
using Airport.Persistence.Configurations;

namespace Airport.Persistence
{
    public sealed class SeedData
    {
        public static async Task InitializeAsync(
            IMongoClient client,
            IAirportDbConfiguration configuration)
        {
            if ((await (await client.ListDatabaseNamesAsync())
                .ToListAsync())
                .Any(name => configuration.DatabaseName == name))
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
            IAirportDbConfiguration configuration)
        {
            if ((await (await client.ListDatabaseNamesAsync())
                .ToListAsync())
                .Any(name => configuration.DatabaseName == name))
                await client.DropDatabaseAsync(configuration.DatabaseName);
        }
    }
}
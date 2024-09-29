using MongoDB.Driver;
using Airport.Contracts.Database;

namespace Airport.Persistence.Configurations
{
    internal class LandingConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IAirportDbConfiguration dbSettings) => 
            await Task.CompletedTask;
    }
}
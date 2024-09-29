using MongoDB.Driver;
using Airport.Contracts.Database;

namespace Airport.Persistence.Configurations
{
    internal class DepartureConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IAirportDbConfiguration dbSettings) => 
            await Task.CompletedTask;
    }
}
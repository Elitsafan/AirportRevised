using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Airport.Persistence.Configurations
{
    internal class DepartureConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration) =>
            await Task.CompletedTask;
    }
}
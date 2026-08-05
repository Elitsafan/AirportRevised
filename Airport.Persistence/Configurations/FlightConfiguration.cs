namespace Airport.Persistence.Configurations
{
    internal class FlightConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            var collection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Flight>(dbConfiguration.Value.FlightsCollectionName);

            var indexKeys = Builders<Flight>.IndexKeys.Ascending(f => f.OccupationDetails[0].Entrance);

            var indexModel = new CreateIndexModel<Flight>(indexKeys);

            await collection.Indexes.CreateOneAsync(indexModel);
        }
    }
}

namespace Airport.Persistence.Configurations
{
    internal class SyncerConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            var syncersCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Syncer>(dbConfiguration.Value.SyncersCollectionName);

            var data = new List<Syncer>
            {
                new()
                {
                    SyncerId = new ObjectId("6a4f72e36988b0913f8a66f4"),
                    Capacity = 5,
                    SectionCriticalOccupations = new()
                    {
                        new()
                        {
                            RouteId = new ObjectId("650abb1ee574435a814d7ec0"),
                            Value = 2
                        },
                        new()
                        {
                            RouteId = new ObjectId("650abb1ee574435a814d7ec1"),
                            Value = 3
                        }
                    },
                }
            };

            await syncersCollection.InsertManyAsync(data);
        }
    }
}
namespace Airport.Persistence.Configurations
{
    internal class SectionConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            var sectionsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Section>(dbConfiguration.Value.SectionsCollectionName);

            var data = new List<Section>
            {
                new()
                {
                    SectionId = new ObjectId("6a28773b808e6905101183eb"),
                    RouteId = new ObjectId("650abb1ee574435a814d7ec0"),
                    SyncerId = new ObjectId("6a4f72e36988b0913f8a66f4"),
                    Origin = new()
                    {
                        new ObjectId("000000000000000000000004"),
                    },
                    SectionOnly = new()
                    {
                        new ObjectId("000000000000000000000005"),
                    },
                    Destination = new()
                    {
                        new ObjectId("000000000000000000000006"),
                        new ObjectId("000000000000000000000007"),
                    }
                },
                new()
                {
                    SectionId = new ObjectId("6a28892c808e6905101183ec"),
                    RouteId = new ObjectId("650abb1ee574435a814d7ec1"),
                    SyncerId = new ObjectId("6a4f72e36988b0913f8a66f4"),
                    Origin = new()
                    {
                        new ObjectId("000000000000000000000006"),
                        new ObjectId("000000000000000000000007"),
                    },
                    SectionOnly = new()
                    {
                        new ObjectId("000000000000000000000008"),
                    },
                    Destination = new()
                    {
                        new ObjectId("000000000000000000000004"),
                    }
                }
            };

            await sectionsCollection.InsertManyAsync(data);
        }
    }
}
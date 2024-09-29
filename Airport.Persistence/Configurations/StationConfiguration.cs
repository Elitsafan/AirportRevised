using MongoDB.Bson;
using MongoDB.Driver;
using Airport.Contracts.Database;
using Airport.Models.Entities;

namespace Airport.Persistence.Configurations
{
    internal class StationConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IAirportDbConfiguration dbSettings)
        {
            var stationsCollection = client
                .GetDatabase(dbSettings.DatabaseName)
                .GetCollection<Station>(dbSettings.StationsCollectionName);
            var data = new List<Station>
            {
                new Station
                {
                    StationId = new ObjectId("000000000000000000000001"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(150),
                },
                new Station
                {
                    StationId = new ObjectId("000000000000000000000002"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(150),
                },
                new Station
                {
                    StationId = new ObjectId("000000000000000000000003"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(150),
                },
                new Station
                {
                    StationId = new ObjectId("000000000000000000000004"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(200),
                },
                new Station
                {
                    StationId = new ObjectId("000000000000000000000005"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(300),
                },
                new Station
                {
                    StationId = new ObjectId("000000000000000000000006"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(250),
                },
                new Station
                {
                    StationId = new ObjectId("000000000000000000000007"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(250),
                },
                new Station
                {
                    StationId = new ObjectId("000000000000000000000008"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(300),
                },
                new Station
                {
                    StationId = new ObjectId("000000000000000000000009"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(150),
                }
            };
            await stationsCollection.InsertManyAsync(data);
        }
    }
}
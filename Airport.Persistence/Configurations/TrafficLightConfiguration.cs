using Airport.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Airport.Persistence.Configurations
{
    internal class TrafficLightConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            var trafficLightsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<TrafficLight>(dbConfiguration.Value.TrafficLightsCollectionName);
            var data = new List<TrafficLight>
            {
                new TrafficLight
                {
                    TrafficLightId = ObjectId.GenerateNewId(),
                    StationId = new ObjectId("000000000000000000000004"),
                },
                new TrafficLight
                {
                    TrafficLightId = ObjectId.GenerateNewId(),
                    StationId = new ObjectId("000000000000000000000006"),
                },
                new TrafficLight
                {
                    TrafficLightId = ObjectId.GenerateNewId(),
                    StationId = new ObjectId("000000000000000000000007"),
                },
                //new TrafficLight
                //{
                //    TrafficLightId = ObjectId.GenerateNewId(),
                //    StationId = new ObjectId("000000000000000000000008"),
                //}
            };
            await trafficLightsCollection.InsertManyAsync(data);
        }
    }
}

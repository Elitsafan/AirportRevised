﻿using Airport.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Airport.Persistence.Configurations
{
    internal class StationConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            var stationsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Station>(dbConfiguration.Value.StationsCollectionName);
            var data = new List<Station>
            {
                new()
                {
                    StationId = new ObjectId("000000000000000000000001"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(800),
                },
                new() 
                {
                    StationId = new ObjectId("000000000000000000000002"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(800),
                },
                new()
                {
                    StationId = new ObjectId("000000000000000000000003"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(800),
                },
                new()
                {
                    StationId = new ObjectId("000000000000000000000004"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(1000),
                },
                new()
                {
                    StationId = new ObjectId("000000000000000000000005"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(450),
                },
                new()
                {
                    StationId = new ObjectId("000000000000000000000006"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(900),
                },
                new()
                {
                    StationId = new ObjectId("000000000000000000000007"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(900),
                },
                new()
                {
                    StationId = new ObjectId("000000000000000000000008"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(800),
                },
                new()
                {
                    StationId = new ObjectId("000000000000000000000009"),
                    EstimatedWaitingTime = TimeSpan.FromMilliseconds(800),
                }
            };
            await stationsCollection.InsertManyAsync(data);
        }
    }
}
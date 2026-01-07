using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Airport.Models.Entities
{
    public class Station
    {
        [BsonId]
        public ObjectId StationId { get; set; }
        [BsonElement("estimated_waiting_time")]
        public TimeSpan EstimatedWaitingTime { get; set; }
    }
}
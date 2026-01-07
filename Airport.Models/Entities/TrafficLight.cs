using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Airport.Models.Entities
{
    public class TrafficLight
    {
        [BsonId]
        public ObjectId TrafficLightId { get; set; }
        [BsonRequired]
        [BsonElement("station_id")]
        public ObjectId StationId { get; set; }
    }
}

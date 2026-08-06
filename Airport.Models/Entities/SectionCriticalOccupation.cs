using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Airport.Models.Entities
{
    public class SectionCriticalOccupation
    {
        [BsonElement("route_id")]
        public ObjectId RouteId { get; set; }
        [BsonElement("occupation")]
        public int Value { get; set; }
    }
}
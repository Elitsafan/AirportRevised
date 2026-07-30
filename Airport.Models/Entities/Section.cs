using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Airport.Models.Entities
{
    public class Section
    {
        [BsonId]
        public ObjectId SectionId { get; set; }

        [BsonRequired]
        [BsonElement("route_id")]
        public ObjectId RouteId { get; set; }

        [BsonRequired]
        [BsonElement("syncer_id")]
        public ObjectId SyncerId { get; set; }

        [BsonRequired]
        [BsonElement("origin")]
        public List<ObjectId> Origin { get; set; } = new();

        [BsonRequired]
        [BsonElement("section_only")]
        public List<ObjectId> SectionOnly { get; set; } = new();

        [BsonRequired]
        [BsonElement("destination")]
        public List<ObjectId> Destination { get; set; } = new();
    }
}

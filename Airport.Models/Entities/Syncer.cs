using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Airport.Models.Entities
{
    public class Syncer
    {
        [BsonId]
        public ObjectId SyncerId { get; set; }
        [BsonElement("capacity")]
        public int Capacity { get; set; }
        [BsonElement("section_critical_occupations")]
        public List<SectionCriticalOccupation> SectionCriticalOccupations { get; set; } = new();
    }
}

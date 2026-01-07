using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Airport.Models.Entities
{
    public class Direction
    {
        [BsonElement("from")]
        public ObjectId From { get; set; }
        [BsonElement("to")]
        public ObjectId To { get; set; }
    }
}

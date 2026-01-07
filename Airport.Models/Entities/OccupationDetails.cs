using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Airport.Models.Entities
{
    public class OccupationDetails
    {
        [BsonElement("station_id")]
        public ObjectId StationId { get; set; }
        [BsonElement("entrance")]
        [BsonDateTimeOptions(DateOnly = false, Kind = DateTimeKind.Local)]
        public DateTime? Entrance { get; set; }
        [BsonElement("exit")]
        [BsonDateTimeOptions(DateOnly = false, Kind = DateTimeKind.Local)]
        public DateTime? Exit { get; set; }
    }
}

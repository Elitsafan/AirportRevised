using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Airport.Models.Entities
{
    [BsonDiscriminator]
    [BsonKnownTypes(typeof(Departure), typeof(Landing))]
    public class Flight
    {
        [BsonId]
        public ObjectId FlightId { get; set; }

        [BsonElement("route_id")]
        public ObjectId? RouteId { get; set; }

        [BsonElement("occupation_details")]
        public List<OccupationDetails> OccupationDetails { get; set; } = new();

        public override bool Equals(object? obj) => obj is Flight flight && FlightId == flight.FlightId;

        public override int GetHashCode() => FlightId.GetHashCode();
    }
}
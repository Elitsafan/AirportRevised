using Airport.Models.Entities;
using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Models
{
    public class FlightSummary
    {
        public ObjectId FlightId { get; init; }
        public FlightType FlightType { get; init; }
        public IEnumerable<OccupationDetails> Stations { get; init; } = Enumerable.Empty<OccupationDetails>();

        public override bool Equals(object? obj) => obj is FlightSummary summary &&
            FlightId == summary.FlightId &&
            FlightType == summary.FlightType;

        public override int GetHashCode() => HashCode.Combine(FlightId, FlightType);
    }
}

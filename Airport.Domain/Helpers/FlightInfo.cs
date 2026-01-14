using Airport.Models.Enums;

namespace Airport.Domain.Helpers
{
    public class FlightInfo : IFlightInfo
    {
        public ObjectId? FlightId { get; init; }
        public FlightType? FlightType { get; init; }
    }
}

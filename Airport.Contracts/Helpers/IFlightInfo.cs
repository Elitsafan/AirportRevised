using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Contracts.Helpers
{
    public interface IFlightInfo
    {
        public ObjectId? FlightId { get; init; }
        public FlightType? FlightType { get; init; }
        public ObjectId RouteId { get; init; }
    }
}
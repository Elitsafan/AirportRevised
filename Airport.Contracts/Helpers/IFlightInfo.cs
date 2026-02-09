using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Contracts.Helpers
{
    public interface IFlightInfo
    {
        ObjectId FlightId { get; }
        FlightType FlightType { get; }
        ObjectId RouteId { get; }
    }
}
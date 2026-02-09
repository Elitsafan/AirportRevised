using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Contracts.EventArgs.StationEventArgs
{
    public interface IStationClearedEventArgs
    {
        ObjectId? CurrentStationId { get; }
        ObjectId? OldStationId { get; }
        ObjectId RouteId { get; }
        ObjectId FlightId { get; }
        FlightType FlightType { get; }
    }
}

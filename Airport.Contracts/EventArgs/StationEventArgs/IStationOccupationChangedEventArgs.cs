using Airport.Contracts.Logics;
using MongoDB.Bson;

namespace Airport.Contracts.EventArgs.StationEventArgs
{
    public interface IStationOccupationChangedEventArgs
    {
        IStationLogic StationLogic { get; }
        ObjectId FlightId { get; }
        ObjectId RouteId { get; }
    }
}
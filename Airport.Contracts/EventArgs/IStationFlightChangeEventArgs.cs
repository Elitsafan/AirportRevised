using Airport.Contracts.Logics;
using MongoDB.Bson;

namespace Airport.Contracts.EventArgs
{
    public interface IStationFlightChangedEventArgs
    {
        IStationLogic StationLogic { get; }
        ObjectId FlightId { get; }
    }
}
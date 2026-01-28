using MongoDB.Bson;

namespace Airport.Contracts.EventArgs
{
    public interface IStationClearedEventArgs : IStationFlightChangedEventArgs
    {
        ObjectId RouteId { get; }
    }
}

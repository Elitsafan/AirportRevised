using MongoDB.Bson;

namespace Airport.Contracts.EventArgs
{
    public interface IStationClearedEventArgs : IStationEventArgs
    {
        ObjectId RouteId { get; }
    }
}

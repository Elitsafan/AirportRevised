using MongoDB.Bson;

namespace Airport.Contracts.EventArgs
{
    public interface IStationClearedEventArgs : IStationStatusEventArgs
    {
        ObjectId RouteId { get; }
    }
}

using MongoDB.Bson;

namespace Airport.Contracts.EventArgs
{
    public interface IStationStatusEventArgs
    {
        ObjectId FlightId { get; }
    }
}
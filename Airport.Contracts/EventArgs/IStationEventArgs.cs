using MongoDB.Bson;

namespace Airport.Contracts.EventArgs
{
    public interface IStationEventArgs
    {
        ObjectId FlightId { get; }
    }
}
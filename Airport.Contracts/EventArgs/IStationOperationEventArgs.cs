using MongoDB.Bson;

namespace Airport.Contracts.EventArgs
{
    public interface IStationOperationEventArgs
    {
        ObjectId StationId { get; }
    }
}

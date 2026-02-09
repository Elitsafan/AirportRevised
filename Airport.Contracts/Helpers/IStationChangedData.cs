using MongoDB.Bson;

namespace Airport.Contracts.Helpers
{
    public interface IStationChangedData
    {
        ObjectId StationId { get; }
        IFlightInfo? Flight { get; }
    }
}

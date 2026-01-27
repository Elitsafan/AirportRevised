using MongoDB.Bson;

namespace Airport.Contracts.Helpers
{
    public interface IStationChangedData
    {
        public ObjectId StationId { get; init; }
        public IFlightInfo? Flight { get; init; }
    }
}

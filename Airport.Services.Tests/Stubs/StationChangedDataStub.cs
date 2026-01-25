using Airport.Contracts.Helpers;

namespace Airport.Services.Tests.Stubs
{
    public class StationChangedDataStub : IStationChangedData
    {
        public ObjectId StationId { get; init; }
        public IFlightInfo? Flight { get; init; }
    }

    public class FlightInfoStub : IFlightInfo
    {
        public ObjectId? FlightId { get; init; }
        public FlightType? FlightType { get; init; }
    }
}

using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.EventArgs.StationEventArgs
{
    internal class StationClearedEventArgs : IStationClearedEventArgs
    {
        public required IStationLogic StationLogic { get; init; }
        public required ObjectId RouteId { get; init; }
        public ObjectId FlightId { get; init; }
    }
}

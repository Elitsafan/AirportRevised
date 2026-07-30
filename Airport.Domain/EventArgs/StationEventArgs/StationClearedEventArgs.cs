using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.EventArgs.StationEventArgs
{
    internal class StationClearedEventArgs : IStationClearedEventArgs
    {
        public ObjectId? CurrentStationId { get; init; }
        public ObjectId? OldStationId { get; init; }
        public ObjectId RouteId { get; init; }
        public ObjectId FlightId { get; init; }
        public FlightType FlightType { get; init; }
    }
}

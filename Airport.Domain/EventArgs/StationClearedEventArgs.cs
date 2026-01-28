namespace Airport.Domain.EventArgs
{
    internal class StationClearedEventArgs : System.EventArgs, IStationClearedEventArgs
    {
        public required IStationLogic StationLogic { get; init; }
        public ObjectId RouteId { get; init; }
        public ObjectId FlightId { get; init; }
    }
}

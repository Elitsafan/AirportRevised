namespace Airport.Domain.EventArgs
{
    internal class StationClearingEventArgs : System.EventArgs, IStationClearingEventArgs
    {
        public required IStationLogic StationLogic { get; init; }
        public ObjectId FlightId { get; init; }
    }
}

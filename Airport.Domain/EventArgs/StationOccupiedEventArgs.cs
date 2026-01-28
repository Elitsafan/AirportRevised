namespace Airport.Domain.EventArgs
{
    internal class StationOccupiedEventArgs : System.EventArgs, IStationOccupiedEventArgs
    {
        public required IStationLogic StationLogic { get; init; }
        public ObjectId FlightId { get; init; }
    }
}
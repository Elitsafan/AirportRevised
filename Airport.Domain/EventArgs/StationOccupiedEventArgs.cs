namespace Airport.Domain.EventArgs
{
    internal class StationOccupiedEventArgs : System.EventArgs, IStationOccupiedEventArgs
    {
        public StationOccupiedEventArgs(ObjectId flightId) => FlightId = flightId;

        public ObjectId FlightId { get; }
    }
}

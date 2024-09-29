namespace Airport.Domain.EventArgs
{
    internal class StationClearingEventArgs : System.EventArgs, IStationClearingEventArgs
    {
        public StationClearingEventArgs(ObjectId flightId) => FlightId = flightId;

        public ObjectId FlightId { get; }
    }
}

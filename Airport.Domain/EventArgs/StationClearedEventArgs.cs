namespace Airport.Domain.EventArgs
{
    internal class StationClearedEventArgs : System.EventArgs, IStationClearedEventArgs
    {
        public StationClearedEventArgs(ObjectId routeId, ObjectId flightId)
        {
            RouteId = routeId;
            FlightId = flightId;
        }

        public ObjectId RouteId { get; }
        public ObjectId FlightId { get; }
    }
}

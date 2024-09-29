namespace Airport.Domain.EventArgs
{
    internal class FlightRunStartedEventArgs : System.EventArgs, IFlightRunStartedEventArgs
    {
        public FlightRunStartedEventArgs(Flight flight, ObjectId routeId)
        {
            Flight = flight;
            RouteId = routeId;
        }

        public Flight Flight { get; }
        public ObjectId RouteId { get; set; }
    }
}
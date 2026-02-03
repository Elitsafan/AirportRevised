using Airport.Contracts.EventArgs.FlightEventArgs;

namespace Airport.Domain.EventArgs.FlightEventArgs
{
    internal class FlightRunStartedEventArgs : IFlightRunStartedEventArgs
    {
        public FlightRunStartedEventArgs(Flight flight, ObjectId routeId)
        {
            Flight = flight ?? throw new ArgumentNullException(nameof(flight));
            RouteId = routeId;
        }

        public Flight Flight { get; }
        public ObjectId RouteId { get; set; }
    }
}
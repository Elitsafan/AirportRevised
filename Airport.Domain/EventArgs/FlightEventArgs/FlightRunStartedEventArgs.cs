using Airport.Contracts.EventArgs.FlightEventArgs;

namespace Airport.Domain.EventArgs.FlightEventArgs
{
    internal class FlightRunStartedEventArgs : IFlightRunStartedEventArgs
    {
        public required Flight Flight { get; init; }
        public ObjectId StationId { get; init; }
        public ObjectId RouteId { get; init; }
    }
}
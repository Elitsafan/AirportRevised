using Airport.Contracts.EventArgs.FlightEventArgs;

namespace Airport.Domain.EventArgs.FlightEventArgs
{
    internal class FlightRunDoneEventArgs : IFlightRunDoneEventArgs
    {
        public required IFlightLogic Flight { get; init; }
    }
}
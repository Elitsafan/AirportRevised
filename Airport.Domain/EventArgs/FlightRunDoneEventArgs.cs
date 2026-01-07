namespace Airport.Domain.EventArgs
{
    internal class FlightRunDoneEventArgs : System.EventArgs, IFlightRunDoneEventArgs
    {
        public FlightRunDoneEventArgs(IFlightLogic flight) =>
            Flight = flight ?? throw new ArgumentNullException(nameof(flight));

        public IFlightLogic Flight { get; }
    }
}
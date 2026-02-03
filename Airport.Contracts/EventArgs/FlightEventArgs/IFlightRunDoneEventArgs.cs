using Airport.Contracts.Logics;

namespace Airport.Contracts.EventArgs.FlightEventArgs
{
    public interface IFlightRunDoneEventArgs
    {
        IFlightLogic Flight { get; }
    }
}

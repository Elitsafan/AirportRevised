using Airport.Contracts.Logics;

namespace Airport.Contracts.EventArgs
{
    public interface IFlightRunDoneEventArgs
    {
        IFlightLogic Flight { get; }
    }
}

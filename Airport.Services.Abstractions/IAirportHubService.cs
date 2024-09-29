using Airport.Contracts.Logics;

namespace Airport.Services.Abstractions
{
    public interface IAirportHubService
    {
        void RegisterFlightRunDone(IFlightLogic flightLogic);
    }
}
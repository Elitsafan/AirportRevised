using Airport.Contracts.Creators;
using Airport.Models.Entities;

namespace Airport.Contracts.Factories
{
    public interface IFlightLogicFactory
    {
        IFlightLogicCreator GetCreator(Flight flight);
    }
}

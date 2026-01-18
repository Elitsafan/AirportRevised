using Airport.Contracts.Creators;
using Airport.Models.Entities;

namespace Airport.Contracts.Factories
{
    public interface IFlightLogicFactory
    {
        Task<IFlightLogicCreator> GetCreatorAsync(Flight flight, CancellationToken ct = default);
    }
}

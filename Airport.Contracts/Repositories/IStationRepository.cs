using Airport.Models.Entities;

namespace Airport.Contracts.Repositories
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<IEnumerable<Station>> GetStationsByRouteAsync(Route route, CancellationToken cancellationToken = default);
    }
}
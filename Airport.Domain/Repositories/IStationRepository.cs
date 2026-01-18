using Airport.Models.Enums;

namespace Airport.Domain.Repositories
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<IEnumerable<Station>> GetStationsByRouteAsync(Route route, CancellationToken ct = default);
        Task<Station> GetStationByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<IEnumerable<ObjectId>> GetExistingStationIdsAsync(
            IEnumerable<ObjectId> ids,
            CancellationToken ct = default);
        Task<UpdateResult> UpdateStationAsync(
            ObjectId id,
            Station modifiedStation,
            CancellationToken ct = default);
        Task<Station> AddStationAsync(Station station, CancellationToken ct = default);
    }
}
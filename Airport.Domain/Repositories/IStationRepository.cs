using Airport.Models.Enums;

namespace Airport.Domain.Repositories
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<Station> GetStationByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<UpdateResult> UpdateStationAsync(
            ObjectId id,
            Station modifiedStation,
            CancellationToken ct = default);
        Task<IEnumerable<Station>> GetStationsByRouteAsync(Route route, CancellationToken ct = default);
        Task<IEnumerable<ObjectId>> GetExistingStationIdsAsync(
            IEnumerable<ObjectId> ids,
            CancellationToken ct = default);
    }
}
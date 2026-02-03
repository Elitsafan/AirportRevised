using Airport.Models.Enums;

namespace Airport.Domain.Repositories
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<UpdateResult> UpdateStationAsync(Station modifiedStation, CancellationToken ct = default);
        Task<IEnumerable<Station>> GetStationsByRouteAsync(Route route, CancellationToken ct = default);
        Task<IEnumerable<ObjectId>> GetExistingStationIdsAsync(
            IEnumerable<ObjectId> ids,
            CancellationToken ct = default);
        Task<IDictionary<ObjectId, int>> GetCommonStationIdsWithCountsAsync(IEnumerable<ObjectId> stationIds, CancellationToken ct = default);
    }
}
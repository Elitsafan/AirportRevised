using Airport.Models.Enums;

namespace Airport.Domain.Repositories
{
    public interface IRouteRepository : IRepository<Route>
    {
        Task<UpdateResult> UpdateRouteAsync(
            Route modifiedRoute,
            bool upsert = false,
            CancellationToken ct = default);
        Task<IEnumerable<Station>> GetStationsBetweenAsync(
            Route route,
            ObjectId start,
            ObjectId end,
            CancellationToken ct = default);
        Task<IEnumerable<Route>> GetRoutesContainStationAsync(ObjectId stationId, CancellationToken ct = default);
        Task<IEnumerable<Route>> GetIntersectedRoutesAsync(Route route, CancellationToken ct = default);
        Task<IEnumerable<Route>> GetIntersectedRoutesAsync(IEnumerable<ObjectId> stationIds, CancellationToken ct = default);
    }
}
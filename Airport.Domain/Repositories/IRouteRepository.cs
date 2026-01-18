using Airport.Models.Enums;

namespace Airport.Domain.Repositories
{
    public interface IRouteRepository : IRepository<Route>
    {
        Task<Route> GetRouteByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<IEnumerable<Station>> GetStationsBetweenAsync(
            Route route,
            ObjectId start,
            ObjectId end,
            CancellationToken ct = default);
        Task<Route> AddRouteAsync(Route route, CancellationToken ct = default);
        Task<UpdateResult> UpdateRouteAsync(
            ObjectId id, 
            Route modifiedRoute, 
            CancellationToken ct = default);
        Task<IEnumerable<Route>> GetRoutesContainStationAsync(ObjectId stationId, CancellationToken ct = default);
        Task<bool> IsExistOnAnyRoutesAsync(
            ObjectId stationId,
            int limit = 1,
            CancellationToken ct = default);
    }
}
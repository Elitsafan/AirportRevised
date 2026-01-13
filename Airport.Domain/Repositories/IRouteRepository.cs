using Airport.Models.Enums;

namespace Airport.Domain.Repositories
{
    public interface IRouteRepository : IRepository<Route>
    {
        Task<Route> GetRouteByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Station>> GetStationsBetweenAsync(
            Route route,
            ObjectId start,
            ObjectId end,
            CancellationToken cancellationToken = default);
        Task<Route> SaveRouteAsync(Route route, CancellationToken cancellationToken = default);
        Task<bool> DeleteRouteAsync(ObjectId id, CancellationToken cancellationToken);
        Task<UpdateResult> UpdateRouteAsync(
            ObjectId id, 
            Route modifiedRoute, 
            CancellationToken cancellationToken = default);
        Task<IEnumerable<Route>> GetRoutesContainStationAsync(ObjectId stationId, CancellationToken cancellationToken = default);
    }
}
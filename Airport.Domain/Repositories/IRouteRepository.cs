namespace Airport.Domain.Repositories
{
    public interface IRouteRepository : IRepository<Route>
    {
        Task<Route> GetByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<Models.Enums.UpdateResult> UpdateRouteAsync(
            Route modifiedRoute,
            IClientSessionHandle? session = null,
            bool upsert = false,
            CancellationToken ct = default);
        Task<IEnumerable<Route>> GetRoutesContainStationAsync(ObjectId stationId, CancellationToken ct = default);
        Task<IEnumerable<Route>> IntersectedRoutesAsync(Route route, CancellationToken ct = default);
        Task<IEnumerable<ObjectId>> IdsOfRoutesContainStationAsync(ObjectId stationId, CancellationToken ct = default);
        Task<Dictionary<ObjectId, List<Direction>>> DirectionsOfRoutesContainStationAsync(ObjectId stationId, CancellationToken ct = default);
        Task<Dictionary<ObjectId, List<Direction>>> GetAllDirectionsAsync(CancellationToken ct = default);
    }
}
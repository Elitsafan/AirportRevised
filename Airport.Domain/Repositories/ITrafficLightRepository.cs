namespace Airport.Domain.Repositories
{
    public interface ITrafficLightRepository : IRepository<TrafficLight>
    {
        Task<IEnumerable<TrafficLight>> GetTrafficLightsByRouteIdAsync(ObjectId routeId, CancellationToken ct = default);
        Task<bool> DeleteByStationIdAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default);
        Task<IEnumerable<TrafficLight>> GetStandaloneTLsAsync(ObjectId routeId, CancellationToken ct = default);
    }
}
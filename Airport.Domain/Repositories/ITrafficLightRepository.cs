namespace Airport.Domain.Repositories
{
    public interface ITrafficLightRepository : IRepository<TrafficLight>
    {
        Task<TrafficLight> AddTrafficLightAsync(TrafficLight trafficLight, CancellationToken ct = default);
        Task<IEnumerable<TrafficLight>> GetTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default);
        /// <summary>
        /// Gets the very next traffic lights that come after <paramref name="id"/>
        /// </summary>
        /// <param name="routeId"></param>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<TrafficLight>> GetNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId id,
            CancellationToken ct = default);
    }
}
namespace Airport.Domain.Repositories
{
    public interface ITrafficLightRepository : IRepository<TrafficLight>
    {
        Task<IEnumerable<TrafficLight>> GetTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the very next traffic lights that come after <paramref name="id"/>
        /// </summary>
        /// <param name="routeId"></param>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<TrafficLight>> GetNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId id,
            CancellationToken cancellationToken = default);
    }
}
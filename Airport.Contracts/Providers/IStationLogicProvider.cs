using Airport.Contracts.Logics;
using MongoDB.Bson;

namespace Airport.Contracts.Providers
{
    public interface IStationLogicProvider
    {
        Task<IEnumerable<IStationLogic>> FindNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId trafficLightId,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// Finds all the <see cref="IStationLogic"/> that belongs to a route with the provided <paramref name="routeId"/>
        /// </summary>
        /// <param name="routeId"></param>
        /// <returns></returns>
        Task<IEnumerable<IStationLogic>> FindStationLogicsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// Finds all the stations that are traffic lights, by <paramref name="routeId"/>
        /// </summary>
        /// <param name="routeId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>The traffic lights collection as a <see cref="IStationLogic"/> collection</returns>
        Task<IEnumerable<IStationLogic>> FindTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default);
        Task<IEnumerable<IStationLogic>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IStationLogic> GetStationLogicByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
    }
}

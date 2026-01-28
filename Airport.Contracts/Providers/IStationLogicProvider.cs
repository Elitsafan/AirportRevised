using Airport.Contracts.EventArgs;
using Airport.Contracts.Helpers;
using Airport.Contracts.Logics;
using Microsoft.VisualStudio.Threading;
using MongoDB.Bson;

namespace Airport.Contracts.Providers
{
    public interface IStationLogicProvider
    {
        event AsyncEventHandler<IStationChangedEventArgs<IStationChangedData>>? AnyStationOccupied;
        event AsyncEventHandler<IStationChangedEventArgs<IStationChangedData>>? AnyStationCleared;
        Task<IEnumerable<IStationLogic>> FindNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId trafficLightId,
            CancellationToken ct = default);
        /// <summary>
        /// Finds all the <see cref="IStationLogic"/> that belongs to a route with the provided <paramref name="routeId"/>
        /// </summary>
        /// <param name="routeId"></param>
        /// <returns></returns>
        Task<IEnumerable<IStationLogic>> FindStationLogicsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default);
        /// <summary>
        /// Finds all the stations that are traffic lights, by <paramref name="routeId"/>
        /// </summary>
        /// <param name="routeId"></param>
        /// <param name="ct"></param>
        /// <returns>The traffic lights collection as a <see cref="IStationLogic"/> collection</returns>
        Task<IEnumerable<IStationLogic>> FindTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default);
        Task<IEnumerable<IStationLogic>> GetAllAsync(CancellationToken ct = default);
        Task<IStationLogic> GetStationLogicByIdAsync(ObjectId id, CancellationToken ct = default);
    }
}

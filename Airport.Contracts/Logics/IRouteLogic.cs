using Microsoft.VisualStudio.Threading;
using MongoDB.Bson;

namespace Airport.Contracts.Logics
{
    public interface IRouteLogic
    {
        ObjectId RouteId { get; }
        string RouteName { get; }
        /// <summary>
        /// Syncs the start of the route
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The semaphore releaser</returns>
        Task<AsyncSemaphore.Releaser> StartRunAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Performs parallel entrances to the stations of the leg, and returns the entered station.
        /// </summary>
        /// <param name="flightLogic">The <see cref="IFlightLogic"/> that enters the stations</param>
        /// <param name="nextLeg">The stations to enter</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The entered station</returns>
        Task<IStationLogic> EnterLegAsync(
            IFlightLogic flightLogic,
            IEnumerable<IStationLogic> nextLeg,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the stations collection whose source station is <paramref name="stationLogic"/>
        /// </summary>
        /// <param name="stationLogic"></param>
        /// <returns></returns>
        IEnumerable<IStationLogic> GetNextLeg(IStationLogic? stationLogic = null);
    }
}

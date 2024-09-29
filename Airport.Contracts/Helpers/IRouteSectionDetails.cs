using MongoDB.Bson;
using Airport.Contracts.Logics;

namespace Airport.Contracts.Helpers
{
    public interface IRouteSectionDetails
    {
        IRouteSection RouteSection { get; }
        /// <summary>
        /// Enters a flight to a station
        /// </summary>
        /// <param name="station"></param>
        /// <param name="flightId"></param>
        /// <param name="trafficLightsCts"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task EnterSectionAsync(
            IStationLogic station,
            ObjectId flightId,
            CancellationTokenSource? trafficLightsCts,
            CancellationToken cancellationToken = default);
    }
}
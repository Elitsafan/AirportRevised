using Airport.Models.Entities;
using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Contracts.Logics
{
    public interface IFlightLogic : IAsyncDisposable
    {
        ObjectId FlightId { get; }
        /// <summary>
        /// The current <see cref="IStationLogic"/> instance that holds the flight
        /// </summary>
        IStationLogic? CurrentStation { get; }
        ObjectId RouteId { get; }
        FlightType FlightType { get; }
        Task RunAsync(CancellationToken ct = default);
        OccupationDetails RegisterStationOccupiedDetails(ObjectId stationId, DateTime entranceTime);
        OccupationDetails RegisterStationClearedDetails(ObjectId stationId, DateTime exitTime);
        Task RaiseFlightRunStartedAsync(ObjectId stationId);
        Task RaiseFlightRunDoneAsync(CancellationToken ct = default);
        /// <summary>
        /// Eliminates other tasks with the <see cref="CancellationToken"/> of the <paramref name="cts"/>
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task ThrowIfCancellationRequestedAsync(CancellationTokenSource? cts);
    }
}

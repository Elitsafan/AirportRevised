using Airport.Contracts.EventArgs;
using Airport.Models.Entities;
using Airport.Models.Enums;
using Microsoft.VisualStudio.Threading;
using MongoDB.Bson;

namespace Airport.Contracts.Logics
{
    public interface IFlightLogic : System.IAsyncDisposable
    {
        /// <summary>
        /// Occures when the flight has started running
        /// </summary>
        event AsyncEventHandler<IFlightRunStartedEventArgs>? FlightRunStarted;
        /// <summary>
        /// Occures when the flight is done running
        /// </summary>
        event AsyncEventHandler<IFlightRunDoneEventArgs>? FlightRunDone;
        /// <summary>
        /// Represents the flight's id
        /// </summary>
        ObjectId FlightId { get; }
        /// <summary>
        /// The current <see cref="IStationLogic"/> that holds the flight
        /// </summary>
        IStationLogic? CurrentStation { get; }
        /// <summary>
        /// Represents the route's id
        /// </summary>
        ObjectId RouteId { get; }
        /// <summary>
        /// Represents the flight's type
        /// </summary>
        FlightType FlightType { get; }
        /// <summary>
        /// Runs through the route
        /// </summary>
        Task RunAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Registers when the flight entered a station
        /// </summary>
        /// <param name="stationId"></param>
        /// <param name="entranceTime"></param>
        /// <returns></returns>
        OccupationDetails RegisterStationOccupiedDetails(ObjectId stationId, DateTime entranceTime);
        /// <summary>
        /// Registers when the flight exited the station
        /// </summary>
        /// <param name="stationId"></param>
        /// <param name="exitTime"></param>
        /// <returns></returns>
        OccupationDetails RegisterStationClearedDetails(ObjectId stationId, DateTime exitTime);
        /// <summary>
        /// Raises the <see cref="IFlightLogic.FlightRunDone"/> event
        /// </summary>
        /// <returns></returns>
        Task RaiseFlightRunDoneAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Eliminates other tasks with the same <see cref="CancellationToken"/> of the <paramref name="cts"/>
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task ThrowIfCancellationRequestedAsync(CancellationTokenSource? cts);
    }
}

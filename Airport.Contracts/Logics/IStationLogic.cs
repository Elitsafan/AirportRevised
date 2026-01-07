using Airport.Contracts.EventArgs;
using Airport.Models.Enums;
using Microsoft.VisualStudio.Threading;
using MongoDB.Bson;

namespace Airport.Contracts.Logics
{
    public interface IStationLogic
    {
        /// <summary>
        /// Invoked when station is occupied
        /// </summary>
        event AsyncEventHandler<IStationOccupiedEventArgs>? StationOccupiedAsync;
        /// <summary>
        /// Invoked when station is clearing
        /// </summary>
        event AsyncEventHandler<IStationClearingEventArgs>? StationClearingAsync;
        /// <summary>
        /// Invoked when station is cleared
        /// </summary>
        event AsyncEventHandler<IStationClearedEventArgs>? StationClearedAsync;
        ObjectId StationId { get; }
        /// <summary>
        /// Represents the <see cref="FlightType"/> of the current flight
        /// </summary>
        FlightType? CurrentFlightType { get; }
        /// <summary>
        /// Gets the time requires to wait on station
        /// </summary>
        TimeSpan EstimatedWaitingTime { get; }
        /// <summary>
        /// Gets the flight's id if there is a flight
        /// </summary>
        ObjectId? CurrentFlightId { get; }
        /// <summary>
        /// Occupies a station with the <paramref name="flightLogic"/>
        /// </summary>
        /// <param name="flightLogic">The flight that occupies the station</param>
        /// <param name="source"></param>
        /// <returns></returns>
        Task<IStationLogic> SetFlightAsync(IFlightLogic flightLogic, CancellationTokenSource? cts = null);
        /// <summary>
        /// Clears the station from the flight
        /// </summary>
        /// <returns></returns>
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
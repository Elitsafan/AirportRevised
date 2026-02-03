using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Contracts.Logics
{
    public interface IStationLogic : IDisposable
    {
        ObjectId StationId { get; }
        /// <summary>
        /// Represents the <see cref="FlightType"/> of the current flight inside the <see cref="IStationLogic"/>
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
        Task ClearAsync(CancellationToken ct = default);
    }
}
using Airport.Models.Enums;

namespace Airport.Domain.Repositories
{
    public interface IFlightRepository : IRepository<Flight>
    {
        Task AddFlightAsync(Flight flight, CancellationToken ct = default);
        Task<UpdateResult> UpdateFlightAsync(Flight flight, bool upsert = true, CancellationToken ct = default);
        /// <summary>
        /// Oreders flight by the earliest entrance
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<Flight>> OrderByEntranceAsync(CancellationToken ct = default);
        /// <summary>
        /// Flights older than <paramref name="timePassed"/> will not be retrieved.
        /// </summary>
        /// <param name="timePassed"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<Flight>> FilterByTimePassedAsync(TimeSpan timePassed, CancellationToken ct = default);
        Task<Flight> GetFlightByIdAsync(ObjectId id, CancellationToken ct = default);
    }
}
using Airport.Models.Entities;

namespace Airport.Contracts.Repositories
{
    public interface IFlightRepository : IRepository<Flight>
    {
        Task AddFlightAsync(Flight flight, CancellationToken cancellationToken = default);
        //Task<IEnumerable<T>> OfTypeAsync<T>(CancellationToken cancellationToken = default) where T : Flight;
        Task<bool> UpdateFlightAsync(Flight flight, bool upsert = true, CancellationToken cancellationToken = default);
        /// <summary>
        /// Oreders flight by the earliest entrance
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<Flight>> OrderByEntranceAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Flights older than <paramref name="timePassed"/> will not be retrieved.
        /// </summary>
        /// <param name="timePassed"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<Flight>> FilterByTimePassedAsync(TimeSpan timePassed, CancellationToken cancellationToken = default);
    }
}
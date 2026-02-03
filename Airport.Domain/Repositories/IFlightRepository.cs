using Airport.Models;
using Airport.Models.Enums;

namespace Airport.Domain.Repositories
{
    public interface IFlightRepository : IRepository<Flight>
    {
        Task AddCompletedFlightAsync(Flight flight);
        Task<UpdateResult> UpdateFlightAsync(Flight flight, bool upsert = false, CancellationToken ct = default);
        /// <summary>
        /// Order flights by the earliest entrance
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<Flight>> OrderByEntranceAsync(CancellationToken ct = default);
        Task<IEnumerable<Flight>> FilterByTimePassedAsync(TimeSpan timePassed, CancellationToken ct = default);
        Task<IPagedList<TResult>> GetPagedFlightsAsync<TResult>(
            Func<Flight, TResult> func,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
            where TResult : class;
        Task FlushAsync(CancellationToken ct = default);
        Task<int> EnforceStorageLimitAsync(CancellationToken ct = default);
    }
}
using Airport.Models;

namespace Airport.Domain.Repositories
{
    public interface IFlightRepository
    {
        Task<IEnumerable<Flight>> GetAllAsync(CancellationToken ct = default);
        Task<Flight> GetByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<Flight> AddOneAsync(Flight flight, CancellationToken ct = default);
        Task<Models.Enums.UpdateResult> UpdateFlightAsync(
            Flight flight,
            bool upsert = false,
            CancellationToken ct = default);
        Task<bool> DeleteOneAsync(ObjectId id, CancellationToken ct = default);
        Task<IEnumerable<Flight>> OrderByEntranceAsync(CancellationToken ct = default);
        Task<IEnumerable<Flight>> FilterByTimePassedAsync(
            TimeSpan timePassed,
            CancellationToken ct = default);
        Task<IPagedList<TResult>> GetPagedFlightsAsync<TResult>(
            Func<Flight, TResult> func,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
            where TResult : class;
        Task AddCompletedFlightAsync(Flight flight);
        Task<long> FlushAsync(CancellationToken ct = default);
        Task<long> EnforceStorageLimitAsync(CancellationToken ct = default);
    }
}
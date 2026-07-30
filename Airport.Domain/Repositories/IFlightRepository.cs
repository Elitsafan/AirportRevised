using Airport.Models;

namespace Airport.Domain.Repositories
{
    public interface IFlightRepository : IRepository<Flight>
    {
        Task<Flight> GetByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<IEnumerable<Flight>> FilterByTimePassedAsync(
            TimeSpan timePassed,
            CancellationToken ct = default);
        Task<IPagedList<TResult>> GetPagedFlightsAsync<TResult>(
            Func<Flight, TResult> func,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
            where TResult : class;
        void AddCompletedFlight(Flight flight);
        Task<long> FlushAsync(IClientSessionHandle? session = null, CancellationToken ct = default);
        Task<long> EnforceStorageLimitAsync(IClientSessionHandle? session = null, CancellationToken ct = default);
    }
}
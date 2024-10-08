namespace Airport.Domain.Repositories
{
    public interface IRouteRepository : IRepository<Route>
    {
        Task<Route> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Station>> GetStationsBetweenAsync(
            Route route,
            ObjectId start,
            ObjectId end,
            CancellationToken cancellationToken = default);
    }
}
namespace Airport.Domain.Repositories
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<IEnumerable<Station>> GetStationsByRouteAsync(Route route, CancellationToken cancellationToken = default);
    }
}
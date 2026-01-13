using Airport.Models.Enums;

namespace Airport.Domain.Repositories
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<IEnumerable<Station>> GetStationsByRouteAsync(
            Route route,
            CancellationToken cancellationToken = default);
        Task<Station> GetStationByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ObjectId>> GetExistingStationIdsAsync(
            IEnumerable<ObjectId> ids,
            CancellationToken cancellationToken = default);
        Task<UpdateResult> UpdateStationAsync(
            ObjectId id,
            Station modifiedStation,
            CancellationToken cancellationToken = default);
        Task<Station> SaveStationAsync(Station station, CancellationToken cancellationToken = default);
        Task<bool> DeleteStationAsync(ObjectId id, CancellationToken cancellationToken = default);
    }
}
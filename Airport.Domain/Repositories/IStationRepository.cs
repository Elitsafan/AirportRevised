namespace Airport.Domain.Repositories
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<Station> GetByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<Models.Enums.UpdateResult> UpdateStationAsync(
            Station modifiedStation,
            IClientSessionHandle? session = null,
            CancellationToken ct = default);
        Task<IEnumerable<Station>> GetStationsByRouteIdAsync(ObjectId routeId, CancellationToken ct = default);
        Task<IEnumerable<ObjectId>> AreExistAsync(
            IEnumerable<ObjectId> ids,
            CancellationToken ct = default);
        Task<Dictionary<ObjectId, int>> GetCommonIdsToCountsAsync(
            IEnumerable<ObjectId> stationIds,
            IEnumerable<ObjectId>? excludeRouteIds = null,
            int count = 1,
            CancellationToken ct = default);
    }
}
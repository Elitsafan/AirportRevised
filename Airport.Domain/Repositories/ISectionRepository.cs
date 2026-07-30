namespace Airport.Domain.Repositories
{
    public interface ISectionRepository : IRepository<Section>
    {
        Task<long> AddManyAsync(IEnumerable<Section> sections, IClientSessionHandle? session = null, CancellationToken ct = default);
        Task<Dictionary<ObjectId, List<Section>>> SectionsContainAsync(ObjectId stationId, CancellationToken ct = default);
        Task<Dictionary<ObjectId, List<Section>>> AllSectionsByRouteIdsAsync(CancellationToken ct = default);
        Task<IEnumerable<Section>> GetByRouteIdAsync(ObjectId routeId, CancellationToken ct = default);
        Task<bool> DeleteByRouteIdAsync(ObjectId routeId, IClientSessionHandle? session = null, CancellationToken ct = default);
        Task<Dictionary<ObjectId, int>> CountStationsBySyncerIdAsync(IEnumerable<ObjectId>? ids = null, CancellationToken ct = default);
    }
}
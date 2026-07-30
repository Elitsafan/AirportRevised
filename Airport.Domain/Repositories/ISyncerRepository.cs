namespace Airport.Domain.Repositories
{
    public interface ISyncerRepository : IRepository<Syncer>
    {
        Task<Syncer> GetByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<long> AddManyAsync(IEnumerable<Syncer> syncers, IClientSessionHandle? session = null, CancellationToken ct = default);
        Task UpdateAfterRemoveRouteIdAsync(ObjectId routeId, IClientSessionHandle? session = null, CancellationToken ct = default);
        Task<IEnumerable<ObjectId>> DeleteIfChildlessAsync(IClientSessionHandle? session = null, CancellationToken ct = default);
        Task<Syncer?> GetSyncerBySectionAsync(Section section, CancellationToken ct = default);
        Task<long> UpdateManyAsync(IEnumerable<Syncer> syncers, IClientSessionHandle? session = null, CancellationToken ct = default);
    }
}

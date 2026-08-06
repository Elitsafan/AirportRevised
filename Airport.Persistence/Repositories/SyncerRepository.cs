using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
using MongoDB.Driver.Linq;

namespace Airport.Persistence.Repositories
{
    internal sealed class SyncerRepository : ISyncerRepository
    {
        #region Fields
        private readonly IMongoCollection<Syncer> _syncersCollection;
        private readonly IMongoCollection<Section> _sectionsCollection;
        #endregion

        public SyncerRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _syncersCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Syncer>(dbConfiguration.Value.SyncersCollectionName);

            _sectionsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Section>(dbConfiguration.Value.SectionsCollectionName);
        }

        public async Task<IEnumerable<Syncer>> GetAllAsync(CancellationToken ct = default) =>
            await _syncersCollection.AsQueryable().ToListAsync(ct);

        public async Task<Syncer> GetByIdAsync(ObjectId id, CancellationToken ct = default) =>
            await _syncersCollection.AsQueryable()
            .FirstOrDefaultAsync(s => s.SyncerId == id, ct)
            ?? throw new EntityNotFoundException($"Syncer Id: {id} not found.");

        public async Task<Syncer> AddOneAsync(Syncer syncer, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (session is null)
                await _syncersCollection.InsertOneAsync(syncer, cancellationToken: ct);
            else
                await _syncersCollection.InsertOneAsync(session, syncer, null, ct);

            return syncer;
        }

        public async Task<long> AddManyAsync(IEnumerable<Syncer> syncers, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (syncers is null)
                throw new ArgumentNullException(nameof(syncers));

            var syncerList = syncers.ToList();

            if (syncerList.Count == 0)
                return 0;

            var writeList = new List<InsertOneModel<Syncer>>(syncerList.Count);

            foreach (var syncer in syncerList)
                writeList.Add(new InsertOneModel<Syncer>(syncer));

            var result = session is null
                ? await _syncersCollection.BulkWriteAsync(writeList, cancellationToken: ct)
                : await _syncersCollection.BulkWriteAsync(session, writeList, null, ct);

            return result.InsertedCount;
        }

        public async Task<long> UpdateManyAsync(IEnumerable<Syncer> syncers, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (syncers is null)
                throw new ArgumentNullException(nameof(syncers));

            var syncerList = syncers.ToList();

            if (syncerList.Count == 0)
                return 0;

            var updateList = new List<UpdateOneModel<Syncer>>(syncerList.Count);

            foreach (var syncer in syncerList)
                updateList.Add(new UpdateOneModel<Syncer>(
                    Builders<Syncer>.Filter.Eq(s => s.SyncerId, syncer.SyncerId),
                    Builders<Syncer>.Update
                        .Set(s => s.Capacity, syncer.Capacity)
                        .Set(s => s.SectionCriticalOccupations, syncer.SectionCriticalOccupations))
                {
                    IsUpsert = true
                });

            var result = session is null
                ? await _syncersCollection.BulkWriteAsync(updateList, cancellationToken: ct)
                : await _syncersCollection.BulkWriteAsync(session, updateList, null, ct);

            return result.Upserts.Count + result.ModifiedCount;
        }

        public async Task UpdateAfterRemoveRouteIdAsync(
            ObjectId routeId,
            IClientSessionHandle? session = null,
            CancellationToken ct = default)
        {
            if (session is null)
                await _syncersCollection.UpdateManyAsync(
                    s => s.SectionCriticalOccupations != null && s.SectionCriticalOccupations.Any(co => co.RouteId == routeId),
                    Builders<Syncer>.Update.PullFilter(
                        s => s.SectionCriticalOccupations,
                        co => co.RouteId == routeId),
                    cancellationToken: ct);
            else
                await _syncersCollection.UpdateManyAsync(
                    session,
                    s => s.SectionCriticalOccupations != null && s.SectionCriticalOccupations.Any(co => co.RouteId == routeId),
                    Builders<Syncer>.Update.PullFilter(
                        s => s.SectionCriticalOccupations,
                        co => co.RouteId == routeId),
                    cancellationToken: ct);

            var update = await _syncersCollection.AsQueryable()
                .GroupJoin(
                    _sectionsCollection,
                    s => s.SyncerId,
                    s => s.SyncerId,
                    (syncer, sections) => new UpdateOneModel<Syncer>(
                        Builders<Syncer>.Filter.Eq(s => s.SyncerId, syncer.SyncerId),
                        Builders<Syncer>.Update
                            .Set(s => s.Capacity, sections.SelectMany(
                                s => s.Origin.Concat(
                                    s.Destination).Concat(s.SectionOnly)).Distinct().Count())
                            .Set(s => s.SectionCriticalOccupations, sections.Select(s => new SectionCriticalOccupation
                            {
                                RouteId = s.RouteId,
                                Value = s.Origin.Count + s.SectionOnly.Count
                            })
                            .ToList())))
                .ToListAsync(ct);

            if (session is null)
                await _syncersCollection.BulkWriteAsync(update, cancellationToken: ct);
            else
                await _syncersCollection.BulkWriteAsync(session, update, null, ct);
        }

        public async Task<bool> DeleteOneAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default) => session is null
            ? (await _syncersCollection.DeleteOneAsync(s => s.SyncerId == id, cancellationToken: ct)).DeletedCount > 0
            : (await _syncersCollection.DeleteOneAsync(session, s => s.SyncerId == id, null, ct)).DeletedCount > 0;

        public async Task<Syncer?> GetSyncerBySectionAsync(Section section, CancellationToken ct = default)
        {
            if (section is null)
                throw new ArgumentNullException(nameof(section));

            var commonSection = await _sectionsCollection.AsQueryable()
                .FirstOrDefaultAsync(
                    s => s.Origin.Intersect(section.Origin).Any() ||
                    s.Destination.Intersect(section.Destination).Any(), ct);

            if (commonSection is null)
                return null;

            return await GetByIdAsync(commonSection.SyncerId, ct);
        }

        // TODO: Fix all redundant ToList()
        public async Task<IEnumerable<ObjectId>> DeleteIfChildlessAsync(IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            var idsToDelete = await _syncersCollection
                .AsQueryable(session)
                .Where(s => s.Capacity == 0)
                .Select(s => s.SyncerId)
                .ToListAsync(ct);

            if (session is null)
                await _syncersCollection.DeleteManyAsync(s => idsToDelete.Contains(s.SyncerId), cancellationToken: ct);
            else
                await _syncersCollection.DeleteManyAsync(session, s => idsToDelete.Contains(s.SyncerId), cancellationToken: ct);

            return idsToDelete;
        }
    }
}

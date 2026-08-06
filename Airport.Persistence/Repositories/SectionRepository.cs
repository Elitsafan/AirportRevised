using Airport.Domain.Repositories;
using Airport.Models.Entities;
using MongoDB.Driver.Linq;

namespace Airport.Persistence.Repositories
{
    internal sealed class SectionRepository : ISectionRepository
    {
        #region Fields
        private readonly IMongoCollection<Section> _sectionsCollection;
        #endregion

        public SectionRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration) => _sectionsCollection = client
            .GetDatabase(dbConfiguration.Value.DatabaseName)
            .GetCollection<Section>(dbConfiguration.Value.SectionsCollectionName);

        public async Task<IEnumerable<Section>> GetAllAsync(CancellationToken ct = default) =>
            await _sectionsCollection.AsQueryable().ToListAsync(ct);

        public async Task<Section> AddOneAsync(Section section, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (session is null)
                await _sectionsCollection.InsertOneAsync(section, cancellationToken: ct);
            else
                await _sectionsCollection.InsertOneAsync(session, section, null, ct);

            return section;
        }

        public async Task<long> AddManyAsync(IEnumerable<Section> sections, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (sections is null)
                throw new ArgumentNullException(nameof(sections));

            var sectionList = sections.ToList();

            if (sectionList.Count == 0)
                return 0;

            var writeList = new List<InsertOneModel<Section>>(sectionList.Count);

            foreach (var section in sectionList)
                writeList.Add(new InsertOneModel<Section>(section));

            var result = session is null
                ? await _sectionsCollection.BulkWriteAsync(writeList, cancellationToken: ct)
                : await _sectionsCollection.BulkWriteAsync(session, writeList, null, ct);

            return result.InsertedCount;
        }

        public async Task<bool> DeleteOneAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default) => session is null
            ? (await _sectionsCollection.DeleteOneAsync(s => s.SectionId == id, cancellationToken: ct)).DeletedCount > 0
            : (await _sectionsCollection.DeleteOneAsync(session, s => s.SectionId == id, null, ct)).DeletedCount > 0;

        public async Task<Dictionary<ObjectId, int>> CountStationsBySyncerIdAsync(
            IEnumerable<ObjectId>? ids = null,
            CancellationToken ct = default)
        {
            var q = _sectionsCollection.AsQueryable();

            if (ids is not null)
                q = q.Where(s => ids.Contains(s.SyncerId));

            return (await q.GroupBy(
                s => s.SyncerId,
                s => s,
                (id, collection) => new KeyValuePair<ObjectId, int>(
                    id,
                    collection.SelectMany(s => s.SectionOnly)
                        .Count() +
                    collection.SelectMany(s => s.Origin.Concat(s.Destination))
                        .Distinct()
                        .Count()))
                .ToListAsync(ct))
                .ToDictionary();
        }

        public async Task<Dictionary<ObjectId, List<Section>>> AllSectionsByRouteIdsAsync(CancellationToken ct = default)
        {
            var sectionsByRoutes = await _sectionsCollection
                .Aggregate()
                .Group(
                    s => s.RouteId,
                    g => new
                    {
                        RouteId = g.Key,
                        Sections = g.Select(s => s).ToList()
                    })
                .ToListAsync(ct);

            return sectionsByRoutes.ToDictionary(sg => sg.RouteId, sg => sg.Sections);
        }

        public async Task<Dictionary<ObjectId, List<Section>>> SectionsContainAsync(ObjectId stationId, CancellationToken ct = default)
        {
            var sectionsGrouped = await _sectionsCollection
                .Aggregate()
                .Match(Builders<Section>.Filter.Or(
                    Builders<Section>.Filter.AnyEq(s => s.Origin, stationId),
                    Builders<Section>.Filter.AnyEq(s => s.Destination, stationId),
                    Builders<Section>.Filter.AnyEq(s => s.SectionOnly, stationId)))
                .Group(
                    s => s.RouteId,
                    g => new
                    {
                        RouteId = g.Key,
                        Sections = g.Select(s => s).ToList()
                    })
                .ToListAsync(ct);

            return sectionsGrouped.ToDictionary(sg => sg.RouteId, sg => sg.Sections);
        }

        public async Task<IEnumerable<Section>> GetByRouteIdAsync(ObjectId routeId, CancellationToken ct = default) =>
            await _sectionsCollection.AsQueryable()
            .Where(s => s.RouteId == routeId)
            .ToListAsync(ct);

        public async Task<bool> DeleteByRouteIdAsync(ObjectId routeId, IClientSessionHandle? session = null, CancellationToken ct = default) => session is null
            ? (await _sectionsCollection.DeleteManyAsync(s => s.RouteId == routeId, cancellationToken: ct)).IsAcknowledged
            : (await _sectionsCollection.DeleteManyAsync(session, s => s.RouteId == routeId, null, ct)).IsAcknowledged;
    }
}

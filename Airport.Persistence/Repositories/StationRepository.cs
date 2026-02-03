using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Airport.Persistence.Repositories
{
    internal sealed class StationRepository : IStationRepository
    {
        #region Fields
        private readonly IMongoCollection<Station> _stationsCollection;
        private readonly IMongoCollection<Route> _routesCollection;
        #endregion

        public StationRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _stationsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Station>(dbConfiguration.Value.StationsCollectionName);
            _routesCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Route>(dbConfiguration.Value.RoutesCollectionName);
        }

        public async Task<IEnumerable<Station>> GetAllAsync(CancellationToken ct = default) =>
            await _stationsCollection
            .Find(Builders<Station>.Filter.Empty)
            .ToListAsync(ct);

        public async Task<Station> GetByIdAsync(ObjectId id, CancellationToken ct = default) =>
            await _stationsCollection
            .Find(s => s.StationId == id)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException($"Station Id: {id} not found.");

        public async Task<Station> AddOneAsync(Station station, CancellationToken ct = default)
        {
            await _stationsCollection.InsertOneAsync(station, null, ct);
            return station;
        }

        public async Task<Models.Enums.UpdateResult> UpdateStationAsync(
            Station modifiedStation,
            CancellationToken ct = default)
        {
            var updateResult = await _stationsCollection.UpdateOneAsync(
                r => r.StationId == modifiedStation.StationId,
                Builders<Station>.Update
                    .Set(nameof(Station.EstimatedWaitingTime), modifiedStation.EstimatedWaitingTime),
                new UpdateOptions { IsUpsert = false },
                ct);
            if (updateResult.MatchedCount < 1)
                return Models.Enums.UpdateResult.Failed;
            if (updateResult.ModifiedCount < 1)
                return Models.Enums.UpdateResult.Matched;
            return Models.Enums.UpdateResult.Modified;
        }

        public async Task<bool> DeleteOneAsync(ObjectId id, CancellationToken ct = default) =>
            (await _stationsCollection.DeleteOneAsync(r => r.StationId == id, ct)).DeletedCount > 0;

        public async Task<IEnumerable<Station>> GetStationsByRouteAsync(
            Route route,
            CancellationToken ct = default)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            var stationIds = route.Directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .ToList();
            return await (_stationsCollection
                .Find(Builders<Station>.Filter.In(s => s.StationId, stationIds))
                .ToListAsync(ct));
        }

        public async Task<IEnumerable<ObjectId>> GetExistingStationIdsAsync(
            IEnumerable<ObjectId> ids,
            CancellationToken ct = default) => await _stationsCollection
                .Find(s => ids.Contains(s.StationId))
                .Project(s => s.StationId)
                .ToListAsync(ct);

        public async Task<IDictionary<ObjectId, int>> GetCommonStationIdsWithCountsAsync(
            IEnumerable<ObjectId> stationIds,
            CancellationToken ct = default)
        {
            var ids = stationIds
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<ObjectId, int>();

            // Fetch any route that shares at least one station (Optimization to reduce data transfer)
            // We only need the Directions to calculate commonality
            var relevantRoutes = await _routesCollection
                .Find(Builders<Route>.Filter.ElemMatch(
                    r => r.Directions,
                    d => ids.Contains(d.From) || ids.Contains(d.To)))
                .Project(r => r.Directions)
                .ToListAsync(ct);

            return relevantRoutes
                // Flatten to unique stations PER ROUTE
                .SelectMany(directions => directions
                    .SelectMany(d => new[] { d.From, d.To })
                    .Distinct())
                // Group by StationId to count how many routes contain it
                .GroupBy(stationId => stationId)
                // Filter: Must be one of our source stations
                // AND appear on multiple routes (Source + Others)
                .Where(g => ids.Contains(g.Key) && g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}

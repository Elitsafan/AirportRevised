using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
using MongoDB.Driver.Linq;

namespace Airport.Persistence.Repositories
{
    internal sealed class StationRepository : IStationRepository
    {
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
            await _stationsCollection.AsQueryable().ToListAsync(ct);

        public async Task<Station> GetByIdAsync(ObjectId id, CancellationToken ct = default) =>
            await _stationsCollection.AsQueryable()
            .FirstOrDefaultAsync(s => s.StationId == id, ct)
            ?? throw new EntityNotFoundException($"Station Id: {id} not found.");

        public async Task<Station> AddOneAsync(Station station, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (session is null)
                await _stationsCollection.InsertOneAsync(station, cancellationToken: ct);
            else
                await _stationsCollection.InsertOneAsync(session, station, null, ct);

            return station;
        }

        public async Task<Models.Enums.UpdateResult> UpdateStationAsync(
            Station modifiedStation,
            IClientSessionHandle? session = null,
            CancellationToken ct = default)
        {
            var updateResult = session is null
                ? await _stationsCollection.UpdateOneAsync(
                    r => r.StationId == modifiedStation.StationId,
                    Builders<Station>.Update
                        .Set(nameof(Station.EstimatedWaitingTime), modifiedStation.EstimatedWaitingTime),
                    new UpdateOptions { IsUpsert = false },
                    ct)
                : await _stationsCollection.UpdateOneAsync(
                    session,
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

        public async Task<bool> DeleteOneAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default) => session is null
            ? (await _stationsCollection.DeleteOneAsync(r => r.StationId == id, cancellationToken: ct)).DeletedCount > 0
            : (await _stationsCollection.DeleteOneAsync(session, r => r.StationId == id, null, ct)).DeletedCount > 0;

        public async Task<IEnumerable<Station>> GetStationsByRouteIdAsync(ObjectId routeId, CancellationToken ct = default)
        {
            var route = await _routesCollection.AsQueryable()
                .FirstOrDefaultAsync(r => r.RouteId == routeId, ct)
                ?? throw new EntityNotFoundException($"Route Id: {routeId} not found.");

            var stationIds = route.Directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .ToList();

            return await _stationsCollection.AsQueryable()
                .Where(s => stationIds.Contains(s.StationId))
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ObjectId>> AreExistAsync(IEnumerable<ObjectId> ids, CancellationToken ct = default) =>
            await _stationsCollection.AsQueryable()
            .Where(s => ids.Contains(s.StationId))
            .Select(s => s.StationId)
            .ToListAsync(ct);

        public async Task<Dictionary<ObjectId, int>> GetCommonIdsToCountsAsync(
            IEnumerable<ObjectId> stationIds,
            IEnumerable<ObjectId>? excludeRouteIds = null,
            int count = 1,
            CancellationToken ct = default)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var ids = stationIds.Distinct().ToList();

            if (ids.Count == 0)
                return new();

            // Fetch any route that shares at least one station (Optimization to reduce data transfer)
            // We only need the Directions to calculate commonality
            var relevantRoutes = excludeRouteIds is null
                ? await _routesCollection
                .AsQueryable()
                .Where(r => r.Directions.Any(d => ids.Contains(d.From) || ids.Contains(d.To)))
                .Select(r => r.Directions)
                .ToListAsync(ct)
                : await _routesCollection
                .AsQueryable()
                .Where(r => !excludeRouteIds.Contains(r.RouteId) &&
                    r.Directions.Any(d => ids.Contains(d.From) || ids.Contains(d.To)))
                .Select(r => r.Directions)
                .ToListAsync(ct);

            return relevantRoutes
                // Flatten to unique stations PER ROUTE
                .SelectMany(directions => directions
                    .SelectMany(d => new[] { d.From, d.To })
                    .Distinct())
                // Group by StationId to count how many routes contain it
                .GroupBy(stationId => stationId)
                // Filter: Must be one of source stations
                // AND appear on multiple routes (Source + Others)
                .Where(g => ids.Contains(g.Key) && g.Count() > count)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<IEnumerable<ObjectId>> GetExistingStationIdsAsync(
            IEnumerable<ObjectId> ids,
            CancellationToken ct = default) => await _stationsCollection
                .Find(s => ids.Contains(s.StationId))
                .Project(s => s.StationId)
                .ToListAsync(ct);
    }
}
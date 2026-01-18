using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Airport.Persistence.Repositories
{
    internal sealed class RouteRepository : IRouteRepository
    {
        #region Fields
        private readonly IMongoCollection<Route> _routesCollection;
        private readonly IOptions<AirportDbConfiguration> _dbConfiguration;
        private readonly IMongoClient _client;
        #endregion

        public RouteRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _client = client;
            _dbConfiguration = dbConfiguration;
            _routesCollection = _client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Route>(dbConfiguration.Value.RoutesCollectionName);
        }

        public async Task<Route> GetRouteByIdAsync(ObjectId id, CancellationToken ct = default) =>
            await _routesCollection
            .Find(r => r.RouteId == id)
            .SingleOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException();

        public async Task<IEnumerable<Route>> GetAllAsync(CancellationToken ct = default) => await _routesCollection
            .Find(Builders<Route>.Filter.Empty)
            .ToListAsync(ct);

        public async Task<IEnumerable<Station>> GetStationsBetweenAsync(
            Route route,
            ObjectId from,
            ObjectId to,
            CancellationToken ct = default)
        {
            // Validations
            if (from == to)
                throw new ArgumentException("Start can not equals to end.");
            var froms = route.Directions
                .Select(d => d.From)
                .Distinct();
            if (froms.All(id => id != from))
                throw new ArgumentException("Id does not exist on route.", nameof(from));
            var tos = route.Directions
                .Select(d => d.To)
                .Distinct();
            if (tos.All(id => id != to))
                throw new ArgumentException("Id does not exist on route.", nameof(to));

            HashSet<ObjectId> stationIds = new();
            var directions = route.Directions
                .Where(d => d.From == from)
                .ToArray();
            if (!await GetStationIdsBetweenAsync(route.Directions, directions, to, stationIds, ct))
                return await Task.FromResult(Enumerable.Empty<Station>());
            var stationsCollection = _client!
                .GetDatabase(_dbConfiguration.Value.DatabaseName)
                .GetCollection<Station>(_dbConfiguration.Value.StationsCollectionName);
            return (await stationsCollection
                .FindAsync(s => stationIds.Contains(s.StationId), cancellationToken: ct))
                .ToList(ct);
        }

        public async Task<Route> AddRouteAsync(Route route, CancellationToken ct = default)
        {
            await _routesCollection.InsertOneAsync(route, null, ct);
            return route;
        }

        public async Task<Models.Enums.UpdateResult> UpdateRouteAsync(
            ObjectId id,
            Route modifiedRoute,
            CancellationToken ct = default)
        {
            var updateResult = await _routesCollection.UpdateOneAsync(
                r => r.RouteId == id,
                Builders<Route>.Update
                    .Set(nameof(Route.RouteName), modifiedRoute.RouteName)
                    .Set(nameof(Route.Directions), modifiedRoute.Directions),
                new UpdateOptions { IsUpsert = false },
                ct);
            if (updateResult.MatchedCount < 1)
                return Models.Enums.UpdateResult.Failed;
            if (updateResult.ModifiedCount < 1)
                return Models.Enums.UpdateResult.Matched;
            return Models.Enums.UpdateResult.Matched | Models.Enums.UpdateResult.Modified;
        }

        public async Task<IEnumerable<Route>> GetRoutesContainStationAsync(
            ObjectId stationId,
            CancellationToken ct = default) => await _routesCollection
            .Find(Builders<Route>.Filter
                .ElemMatch(
                    r => r.Directions,
                    d => d.From == stationId || d.To == stationId))
            .ToListAsync(ct);

        public async Task<bool> IsExistOnAnyRoutesAsync(
            ObjectId stationId,
            int limit = 1,
            CancellationToken ct = default)
        {
            if (limit < 0) 
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be non-negative.");
            return await _routesCollection
                .Find(Builders<Route>.Filter
                    .ElemMatch(
                        r => r.Directions,
                        d => d.From == stationId || d.To == stationId))
                .Limit(limit)
                .AnyAsync(ct);
        }

        public async Task<bool> DeleteOneAsync(
            ObjectId id,
            CancellationToken ct = default) =>
            (await _routesCollection.DeleteOneAsync(r => r.RouteId == id, ct)).DeletedCount > 0;

        private async Task<bool> GetStationIdsBetweenAsync(
            List<Direction> allDirections,
            Direction[] directions,
            ObjectId to,
            HashSet<ObjectId> ids,
            CancellationToken ct = default)
        {
            if (directions.Length == 0)
                return false;
            if (directions.Any(d => d.To == to))
                return true;
            Direction[] nextDirections = directions
                .Join(
                    allDirections,
                    d => d.To,
                    ad => ad.From,
                    (l, r) => r)
                .ToArray();
            bool added = false;
            foreach (var direction in nextDirections)
                if (await GetStationIdsBetweenAsync(allDirections, nextDirections, direction.To, ids, ct))
                {
                    ids.Add(direction.To);
                    added = true;
                }
            return added;
        }
    }
}

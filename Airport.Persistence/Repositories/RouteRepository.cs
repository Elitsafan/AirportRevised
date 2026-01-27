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
        private readonly IMongoCollection<Station> _stationsCollection;
        #endregion

        public RouteRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _routesCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Route>(dbConfiguration.Value.RoutesCollectionName);
            _stationsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Station>(dbConfiguration.Value.StationsCollectionName);
        }

        public async Task<IEnumerable<Route>> GetAllAsync(CancellationToken ct = default) => await _routesCollection
            .Find(Builders<Route>.Filter.Empty)
            .ToListAsync(ct);

        public async Task<Route> GetRouteByIdAsync(ObjectId id, CancellationToken ct = default) =>
            await _routesCollection
            .Find(r => r.RouteId == id)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException($"Route with Id: {id} not found.");

        public async Task<Route> AddOneAsync(Route route, CancellationToken ct = default)
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
                    .Set(r => r.RouteName, modifiedRoute.RouteName)
                    .Set(r => r.Directions, modifiedRoute.Directions),
                new UpdateOptions { IsUpsert = false },
                ct);
            if (updateResult.MatchedCount < 1)
                return Models.Enums.UpdateResult.Failed;
            if (updateResult.ModifiedCount < 1)
                return Models.Enums.UpdateResult.Matched;
            return Models.Enums.UpdateResult.Modified;
        }

        public async Task<bool> DeleteOneAsync(
            ObjectId id,
            CancellationToken ct = default) =>
            (await _routesCollection.DeleteOneAsync(r => r.RouteId == id, ct)).DeletedCount > 0;

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
                .Project(r => r.RouteId)
                .Limit(limit)
                .AnyAsync(ct);
        }

        public async Task<IEnumerable<Route>> GetRoutesContainStationAsync(
            ObjectId stationId,
            CancellationToken ct = default) => await _routesCollection
            .Find(Builders<Route>.Filter
                .ElemMatch(
                    r => r.Directions,
                    d => d.From == stationId || d.To == stationId))
            .ToListAsync(ct);

        public async Task<IEnumerable<Station>> GetStationsBetweenAsync(
            Route route,
            ObjectId from,
            ObjectId to,
            CancellationToken ct = default)
        {
            var stationIds = new HashSet<ObjectId>();
            var fromDirections = route.Directions
                .Where(d => d.From == from)
                .Distinct()
                .ToList();

            if (FindPath(route.Directions, fromDirections, to, stationIds))
                return await _stationsCollection
                    .Find(Builders<Station>.Filter.In(s => s.StationId, stationIds))
                    .ToListAsync(ct);
            return Enumerable.Empty<Station>();
        }

        private async Task<bool> GetStationIdsBetweenAsync(
            List<Direction> allDirections,
            Direction[] froms,
            ObjectId to,
            HashSet<ObjectId> ids,
            CancellationToken ct = default)
        {
            if (froms.Length == 0)
                return false;
            if (froms.Any(d => d.To == to))
                return true;
            var nextDirections = froms
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

        private bool FindPath(
            List<Direction> allDirections,
            List<Direction> currentLeg,
            ObjectId target,
            HashSet<ObjectId> pathIds)
        {
            if (currentLeg.Count == 0)
                return false;
            if (currentLeg.Any(d => d.To == target))
                return true;

            bool pathFound = false;
            foreach (var direction in currentLeg)
            {
                var nextLeg = allDirections
                    .Where(d => d.From == direction.To)
                    .ToList();
                if (FindPath(allDirections, nextLeg, target, pathIds))
                {
                    pathIds.Add(direction.To);
                    pathFound = true;
                }
            }
            return pathFound;
        }
    }
}
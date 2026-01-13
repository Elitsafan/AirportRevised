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

        public async Task<Route> GetRouteByIdAsync(ObjectId id, CancellationToken cancellationToken = default) =>
            await _routesCollection
            .Find(r => r.RouteId == id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntityNotFoundException();

        public async Task<IEnumerable<Route>> GetAllAsync(CancellationToken cancellationToken = default) => await _routesCollection
            .Find(Builders<Route>.Filter.Empty)
            .ToListAsync(cancellationToken);

        public async Task<IEnumerable<Station>> GetStationsBetweenAsync(
            Route route,
            ObjectId from,
            ObjectId to,
            CancellationToken cancellationToken = default)
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
            if (!await GetStationIdsBetweenAsync(route.Directions, directions, to, stationIds, cancellationToken))
                return await Task.FromResult(Enumerable.Empty<Station>());
            var stationsCollection = _client!
                .GetDatabase(_dbConfiguration.Value.DatabaseName)
                .GetCollection<Station>(_dbConfiguration.Value.StationsCollectionName);
            return (await stationsCollection
                .FindAsync(s => stationIds.Contains(s.StationId), cancellationToken: cancellationToken))
                .ToList(cancellationToken);
        }

        private async Task<bool> GetStationIdsBetweenAsync(
            List<Direction> allDirections,
            Direction[] directions,
            ObjectId to,
            HashSet<ObjectId> ids,
            CancellationToken cancellationToken = default)
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
                if (await GetStationIdsBetweenAsync(allDirections, nextDirections, direction.To, ids, cancellationToken))
                {
                    ids.Add(direction.To);
                    added = true;
                }
            return added;
        }

        public async Task<Route> SaveRouteAsync(Route route, CancellationToken cancellationToken = default)
        {
            await _routesCollection.InsertOneAsync(route, null, cancellationToken);
            return route;
        }

        public async Task<bool> DeleteRouteAsync(
            ObjectId id,
            CancellationToken cancellationToken = default) =>
            (await _routesCollection.DeleteOneAsync(r => r.RouteId == id, cancellationToken)).DeletedCount > 0;

        public async Task<Models.Enums.UpdateResult> UpdateRouteAsync(
            ObjectId id,
            Route modifiedRoute,
            CancellationToken cancellationToken = default)
        {
            var updateResult = await _routesCollection.UpdateOneAsync(
                r => r.RouteId == id,
                Builders<Route>.Update
                    .Set(nameof(Route.RouteName), modifiedRoute.RouteName)
                    .Set(nameof(Route.Directions), modifiedRoute.Directions),
                new UpdateOptions { IsUpsert = false },
                cancellationToken);
            if (updateResult.MatchedCount < 1)
                return Models.Enums.UpdateResult.Failed;
            if (updateResult.ModifiedCount < 1)
                return Models.Enums.UpdateResult.Matched;
            return Models.Enums.UpdateResult.Matched | Models.Enums.UpdateResult.Modified;
        }

        public async Task<IEnumerable<Route>> GetRoutesContainStationAsync(
            ObjectId stationId,
            CancellationToken cancellationToken = default) => await _routesCollection
            .Find(Builders<Route>.Filter
                .ElemMatch(
                    r => r.Directions,
                    d => d.From == stationId || d.To == stationId))
            .ToListAsync(cancellationToken);
    }
}

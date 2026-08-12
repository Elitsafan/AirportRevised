using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
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
            .AsQueryable()
            .ToListAsync(ct);

        public async Task<Route> GetByIdAsync(ObjectId id, CancellationToken ct = default) =>
            await _routesCollection.AsQueryable()
            .FirstOrDefaultAsync(r => r.RouteId == id, ct)
            ?? throw new EntityNotFoundException($"Route Id: {id} not found.");

        public async Task<Route> AddOneAsync(Route route, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (session is null)
                await _routesCollection.InsertOneAsync(route, cancellationToken: ct);
            else
                await _routesCollection.InsertOneAsync(session, route, null, ct);

            return route;
        }

        public async Task<Models.Enums.UpdateResult> UpdateRouteAsync(
            Route modifiedRoute,
            IClientSessionHandle? session = null,
            bool upsert = false,
            CancellationToken ct = default)
        {
            var updateResult = session is null
                ? await _routesCollection.UpdateOneAsync(
                    r => r.RouteId == modifiedRoute.RouteId,
                    Builders<Route>.Update
                        .Set(r => r.RouteName, modifiedRoute.RouteName)
                        .Set(r => r.Directions, modifiedRoute.Directions),
                    new UpdateOptions { IsUpsert = upsert },
                    ct)
                : await _routesCollection.UpdateOneAsync(
                    session,
                    r => r.RouteId == modifiedRoute.RouteId,
                    Builders<Route>.Update
                        .Set(r => r.RouteName, modifiedRoute.RouteName)
                        .Set(r => r.Directions, modifiedRoute.Directions),
                    new UpdateOptions { IsUpsert = upsert },
                    ct);

            if (updateResult.MatchedCount < 1)
                return Models.Enums.UpdateResult.Failed;

            if (updateResult.ModifiedCount < 1)
                return Models.Enums.UpdateResult.Matched;

            return Models.Enums.UpdateResult.Modified;
        }

        public async Task<bool> DeleteOneAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default) => session is null
            ? (await _routesCollection.DeleteOneAsync(r => r.RouteId == id, cancellationToken: ct)).DeletedCount > 0
            : (await _routesCollection.DeleteOneAsync(session, r => r.RouteId == id, null, ct)).DeletedCount > 0;

        public async Task<Dictionary<ObjectId, List<Direction>>> GetAllDirectionsAsync(CancellationToken ct = default) => (await _routesCollection
            .AsQueryable()
            .Select(r => new KeyValuePair<ObjectId, List<Direction>>(r.RouteId, r.Directions))
            .ToListAsync(ct))
            .ToDictionary();

        public async Task<IEnumerable<ObjectId>> IdsOfRoutesContainStationAsync(
            ObjectId stationId,
            CancellationToken ct = default) => await _routesCollection.AsQueryable()
            .Where(r => r.Directions.Any(d => d.From == stationId || d.To == stationId))
            .Select(r => r.RouteId)
            .ToListAsync(ct);

        public async Task<IEnumerable<Route>> GetRoutesContainStationAsync(
            ObjectId stationId,
            CancellationToken ct = default) => await _routesCollection.AsQueryable()
            .Where(r => r.Directions.Any(d => d.From == stationId || d.To == stationId))
            .ToListAsync(ct);

        public async Task<Dictionary<ObjectId, List<Direction>>> DirectionsOfRoutesContainStationAsync(ObjectId stationId, CancellationToken ct = default) =>
            (await _routesCollection.AsQueryable()
                .Where(r => r.Directions.Any(d => d.From == stationId || d.To == stationId))
                .Select(r => new
                {
                    r.RouteId,
                    r.Directions
                })
                .ToListAsync(ct))
            .ToDictionary(
                item => item.RouteId,
                item => item.Directions);

        public async Task<IEnumerable<Route>> IntersectedRoutesAsync(Route route, CancellationToken ct = default)
        {
            var stationIds = route.Directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .ToList();

            return stationIds.Count == 0
                ? Enumerable.Empty<Route>()
                : await _routesCollection
                .Find(
                    Builders<Route>.Filter.And(
                        Builders<Route>.Filter.Ne(r => r.RouteId, route.RouteId),
                        Builders<Route>.Filter.ElemMatch(
                            r => r.Directions,
                            d => stationIds.Contains(d.From) || stationIds.Contains(d.To))))
                .ToListAsync(ct);
        }
    }
}

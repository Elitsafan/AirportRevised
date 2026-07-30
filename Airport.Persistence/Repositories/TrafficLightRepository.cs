using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
using MongoDB.Driver.Linq;

namespace Airport.Persistence.Repositories
{
    internal sealed class TrafficLightRepository : ITrafficLightRepository
    {
        #region Fields
        private readonly IMongoCollection<TrafficLight> _trafficLightsCollection;
        private readonly IMongoCollection<Route> _routesCollection;
        private readonly IMongoCollection<Section> _sectionsCollection;
        #endregion

        public TrafficLightRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _trafficLightsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<TrafficLight>(dbConfiguration.Value.TrafficLightsCollectionName);

            _routesCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Route>(dbConfiguration.Value.RoutesCollectionName);

            _sectionsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Section>(dbConfiguration.Value.SectionsCollectionName);
        }

        public async Task<IEnumerable<TrafficLight>> GetAllAsync(CancellationToken ct = default) =>
            await _trafficLightsCollection.AsQueryable().ToListAsync(ct);

        public async Task<TrafficLight> AddOneAsync(TrafficLight trafficLight, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (session is null)
                await _trafficLightsCollection.InsertOneAsync(trafficLight, cancellationToken: ct);
            else
                await _trafficLightsCollection.InsertOneAsync(session, trafficLight, null, ct);

            return trafficLight;
        }

        public async Task<bool> DeleteOneAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default) => session is null
            ? (await _trafficLightsCollection.DeleteOneAsync(tl => tl.TrafficLightId == id, cancellationToken: ct)).DeletedCount > 0
            : (await _trafficLightsCollection.DeleteOneAsync(session, tl => tl.TrafficLightId == id, null, ct)).DeletedCount > 0;

        public async Task<bool> DeleteByStationIdAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default) => session is null
            ? (await _trafficLightsCollection.DeleteOneAsync(tl => tl.StationId == id, cancellationToken: ct)).DeletedCount > 0
            : (await _trafficLightsCollection.DeleteOneAsync(session, tl => tl.StationId == id, null, ct)).DeletedCount > 0;

        public async Task<IEnumerable<TrafficLight>> GetTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default)
        {
            var stationIds = _routesCollection.AsQueryable()
                .FirstOrDefault(r => r.RouteId == routeId)?.Directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .ToList()
                ?? throw new EntityNotFoundException($"Route id: {routeId} not found.");

            if (stationIds.Count == 0)
                throw new EntityNotFoundException($"Route Id: {routeId} has no stations.");

            return await _trafficLightsCollection.AsQueryable()
                .Where(tl => stationIds.Contains(tl.StationId))
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TrafficLight>> GetStandaloneTLsAsync(ObjectId routeId, CancellationToken ct = default)
        {
            var stationIds = _routesCollection.AsQueryable()
                .FirstOrDefault(r => r.RouteId == routeId)?.Directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .ToList()
                ?? throw new EntityNotFoundException($"Route id: {routeId} not found.");

            if (stationIds.Count == 0)
                throw new EntityNotFoundException($"Route Id: {routeId} has no stations.");

            var tlIdsOfSections = _sectionsCollection.AsQueryable()
                .Where(s => s.RouteId == routeId)
                .SelectMany(s => s.Origin.Concat(s.Destination));

            return await _trafficLightsCollection.AsQueryable()
                .Where(tl => stationIds.Contains(tl.StationId))
                .ExceptBy(tlIdsOfSections, tl => tl.StationId)
                .ToListAsync(ct);
        }
    }
}

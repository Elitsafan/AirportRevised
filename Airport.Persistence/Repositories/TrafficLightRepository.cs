using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Airport.Persistence.Repositories
{
    internal sealed class TrafficLightRepository : ITrafficLightRepository
    {
        #region Fields
        private readonly IOptions<AirportDbConfiguration> _dbConfiguration;
        private readonly IMongoCollection<TrafficLight> _trafficLightsCollection;
        private readonly IMongoClient _client;
        #endregion

        public TrafficLightRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _client = client;
            _dbConfiguration = dbConfiguration;
            _trafficLightsCollection = _client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<TrafficLight>(dbConfiguration.Value.TrafficLightsCollectionName);
        }

        public async Task<TrafficLight> AddTrafficLightAsync(TrafficLight trafficLight, CancellationToken ct = default)
        {
            await _trafficLightsCollection.InsertOneAsync(trafficLight, null, ct);
            return trafficLight;
        }

        public async Task<IEnumerable<TrafficLight>> GetAllAsync(CancellationToken ct = default) =>
            await _trafficLightsCollection
                .Find(Builders<TrafficLight>.Filter.Empty)
                .ToListAsync(ct);

        public async Task<IEnumerable<TrafficLight>> GetTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default)
        {
            var routesCollection = _client!
                .GetDatabase(_dbConfiguration.Value.DatabaseName)
                .GetCollection<Route>(_dbConfiguration.Value.RoutesCollectionName);
            var stationIds = (await routesCollection
                .Find(r => r.RouteId == routeId)
                .SingleAsync(ct)).Directions
                .SelectMany(d => new ObjectId[] { d.From, d.To })
                .Distinct();

            return await _trafficLightsCollection
                .Find(Builders<TrafficLight>.Filter.In(x => x.StationId, stationIds))
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TrafficLight>> GetNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId id,
            CancellationToken ct = default)
        {
            var routesCollection = _client
                .GetDatabase(_dbConfiguration.Value.DatabaseName)
                .GetCollection<Route>(_dbConfiguration.Value.RoutesCollectionName);
            var route = await routesCollection
                .Find(r => r.RouteId == routeId)
                .FirstOrDefaultAsync(ct)
                ?? throw new EntityNotFoundException($"Route Id: {routeId} not found");
            var trafficLightCollection = _client
                .GetDatabase(_dbConfiguration.Value.DatabaseName)
                .GetCollection<TrafficLight>(_dbConfiguration.Value.TrafficLightsCollectionName);
            var tls = await GetTrafficLightsByRouteIdAsync(routeId, ct);
            var trafficLight = await (await trafficLightCollection
                .FindAsync(tl => tl.TrafficLightId == id, cancellationToken: ct))
                .FirstOrDefaultAsync(ct);
            if (trafficLight is null)
            {
                if (tls.All(tl => tl.StationId != id))
                    throw new ArgumentException(
                        "The route with the provided id doesn't have the traffic light with the provided station id.",
                        nameof(id));
                // If id provided is a station id 
                return (await GetNextTrafficLightsAsync(route, id, ct))
                    .ToArray();
            }
            // If id provided is a traffic light id 
            return (await GetNextTrafficLightsAsync(route, trafficLight.StationId, ct))
                .ToArray();
        }

        private async Task<IEnumerable<TrafficLight>> GetNextTrafficLightsAsync(
            Route route,
            ObjectId stationId,
            CancellationToken ct = default)
        {
            var nextDirections = route.Directions
                .Where(d => d.From == stationId)
                .ToArray();
            TrafficLight[] trafficLights = Array.Empty<TrafficLight>();
            if (nextDirections.Length == 0)
                return trafficLights;
            var tasks = nextDirections
                .Select(async d => await GetTrafficLightByStationIdAsync(d.To, ct));
            trafficLights = (await Task.WhenAll(tasks))
                .Where(tl => tl is not null)
                .ToArray();
            if (trafficLights.Length > 0)
                return trafficLights;
            return (await Task.WhenAll(nextDirections
                .Select(async d => await GetNextTrafficLightsAsync(route, d.To, ct))))
                .SelectMany(x => x)
                .ToArray();
        }

        private async Task<TrafficLight> GetTrafficLightByStationIdAsync(
            ObjectId stationId,
            CancellationToken ct = default) => await _trafficLightsCollection
            .FindSync(tl => tl.StationId == stationId, cancellationToken: ct)
            .FirstOrDefaultAsync(ct);
        public async Task<bool> DeleteOneAsync(ObjectId id, CancellationToken ct = default) =>
            (await _trafficLightsCollection.DeleteOneAsync(tl => tl.TrafficLightId == id, ct)).DeletedCount > 0;
    }
}

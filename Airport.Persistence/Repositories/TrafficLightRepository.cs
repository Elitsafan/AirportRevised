using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Airport.Persistence.Repositories
{
    internal sealed class TrafficLightRepository : ITrafficLightRepository
    {
        #region Fields
        private readonly IMongoCollection<TrafficLight> _trafficLightsCollection;
        private readonly IMongoCollection<Route> _routesCollection;
        #endregion

        public TrafficLightRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _trafficLightsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<TrafficLight>(dbConfiguration.Value.TrafficLightsCollectionName);
            _routesCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Route>(dbConfiguration.Value.RoutesCollectionName);
        }

        public async Task<IEnumerable<TrafficLight>> GetAllAsync(CancellationToken ct = default) =>
            await _trafficLightsCollection
                .Find(FilterDefinition<TrafficLight>.Empty)
                .ToListAsync(ct);

        public async Task<TrafficLight> GetByIdAsync(ObjectId id, CancellationToken ct = default) =>
            await _trafficLightsCollection
            .Find(tl => tl.TrafficLightId == id)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException($"Traffic light Id: {id} not found.");

        public async Task<TrafficLight> AddOneAsync(TrafficLight trafficLight, CancellationToken ct = default)
        {
            await _trafficLightsCollection.InsertOneAsync(trafficLight, null, ct);
            return trafficLight;
        }

        public async Task<bool> DeleteOneAsync(ObjectId id, CancellationToken ct = default) =>
            (await _trafficLightsCollection.DeleteOneAsync(tl => tl.TrafficLightId == id, ct)).DeletedCount > 0;

        public async Task<IEnumerable<TrafficLight>> GetNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId id,
            CancellationToken ct = default)
        {
            var route = await _routesCollection
                .Find(r => r.RouteId == routeId)
                .FirstOrDefaultAsync(ct)
                ?? throw new EntityNotFoundException($"Route Id: {routeId} not found.");

            var tls = await GetTrafficLightsByRouteIdAsync(routeId, ct);
            var trafficLight = await _trafficLightsCollection
                .Find(tl => tl.TrafficLightId == id)
                .FirstOrDefaultAsync(ct);
            if (trafficLight is null)
                if (tls.All(tl => tl.StationId != id))
                    throw new ArgumentException(
                        "The route with the provided id doesn't have the traffic light with the provided station id.",
                        nameof(id));
                // If id provided is a station id 
                else return (await GetNextTrafficLightsAsync(route, id, ct))
                    .ToList();
            // If id provided is a trafficlight id 
            return (await GetNextTrafficLightsAsync(route, trafficLight.StationId, ct))
                .ToList();
        }

        public async Task<IEnumerable<TrafficLight>> GetTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default)
        {
            var stationIds = await _routesCollection.AsQueryable()
                .Where(r => r.RouteId == routeId)
                .SelectMany(r => r.Directions
                    .SelectMany(d => new[] { d.From, d.To })
                    .Distinct())
                .Distinct()
                .ToListAsync();
            var result = await _trafficLightsCollection
                .Find(Builders<TrafficLight>.Filter.In(
                    tl => tl.StationId,
                    stationIds))
                .ToListAsync(ct);

            if (result.Count == 0 && !await _routesCollection
                .Find(r => r.RouteId == routeId)
                .AnyAsync(ct))
                throw new EntityNotFoundException($"Route Id: {routeId} not found.");

            return result;
        }

        private async Task<IEnumerable<TrafficLight>> GetNextTrafficLightsAsync(
            Route route,
            ObjectId stationId,
            CancellationToken ct = default)
        {
            var nextDirections = route.Directions
                .Where(d => d.From == stationId)
                .ToArray();
            if (nextDirections.Length == 0)
                return Enumerable.Empty<TrafficLight>();

            var targetStationsIds = nextDirections
                .Select(d => d.To)
                .Distinct()
                .ToList();
            var trafficLights = await _trafficLightsCollection
                .Find(Builders<TrafficLight>.Filter.In(tl => tl.StationId, targetStationsIds))
                .ToListAsync();
            if (trafficLights.Count > 0)
                return trafficLights;
            // If no trafficlights were found
            return (await Task.WhenAll(nextDirections
                .Select(async d => await GetNextTrafficLightsAsync(route, d.To, ct))))
                .SelectMany(x => x)
                .Distinct()
                .ToList();
        }
    }
}

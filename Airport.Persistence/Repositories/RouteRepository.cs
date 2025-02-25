﻿using Airport.Domain.Repositories;
using Airport.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

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

        public async Task<Route> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default) =>
            await _routesCollection
            .Find(r => r.RouteId == id)
            .SingleOrDefaultAsync(cancellationToken);

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
    }
}

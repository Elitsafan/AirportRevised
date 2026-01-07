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
        private readonly IMongoClient _client;
        #endregion

        public StationRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _client = client;
            _stationsCollection = _client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Station>(dbConfiguration.Value.StationsCollectionName);
            _routesCollection = _client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Route>(dbConfiguration.Value.RoutesCollectionName);
        }

        public async Task<IEnumerable<Station>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _stationsCollection
            .Find(Builders<Station>.Filter.Empty)
            .ToListAsync(cancellationToken);

        public async Task<IEnumerable<Station>> GetStationsByRouteAsync(
            Route route,
            CancellationToken cancellationToken = default)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));
            var stationIds = route.Directions
                .SelectMany(d => new ObjectId[] { d.From, d.To })
                .Distinct();
            var filter = Builders<Station>.Filter.In(nameof(Station.StationId), stationIds);
            return await _stationsCollection
                .Find(filter)
                .ToListAsync(cancellationToken);
        }
    }
}

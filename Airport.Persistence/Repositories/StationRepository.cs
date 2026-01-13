using Airport.Domain.Exceptions;
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

        public async Task<Station> GetStationByIdAsync(ObjectId id, CancellationToken cancellationToken = default) =>
            await _stationsCollection
            .Find(s => s.StationId == id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new EntityNotFoundException();

        public async Task<IEnumerable<ObjectId>> GetExistingStationIdsAsync(
            IEnumerable<ObjectId> ids,
            CancellationToken cancellationToken = default) =>
            await _stationsCollection
                .Find(s => ids.Contains(s.StationId))
                .Project(s => s.StationId)
                .ToListAsync(cancellationToken);

        public async Task<Station> SaveStationAsync(Station station, CancellationToken cancellationToken = default)
        { 
            await _stationsCollection.InsertOneAsync(station, null, cancellationToken);
            return station;
        }

        public async Task<bool> DeleteStationAsync(ObjectId id, CancellationToken cancellationToken = default) =>
            (await _stationsCollection.DeleteOneAsync(r => r.StationId == id, cancellationToken)).DeletedCount > 0;

        public async Task<Models.Enums.UpdateResult> UpdateStationAsync(
            ObjectId id,
            Station modifiedStation,
            CancellationToken cancellationToken = default)
        {
            var updateResult = await _stationsCollection.UpdateOneAsync(
                r => r.StationId == id,
                Builders<Station>.Update
                    .Set(nameof(Station.EstimatedWaitingTime), modifiedStation.EstimatedWaitingTime),
                new UpdateOptions { IsUpsert = false },
                cancellationToken);
            if (updateResult.MatchedCount < 1)
                return Models.Enums.UpdateResult.Failed;
            if (updateResult.ModifiedCount < 1)
                return Models.Enums.UpdateResult.Matched;
            return Models.Enums.UpdateResult.Matched | Models.Enums.UpdateResult.Modified;
        }
    }
}

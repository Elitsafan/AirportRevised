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

        public async Task<IEnumerable<Station>> GetAllAsync(CancellationToken ct = default) =>
            await _stationsCollection
            .Find(Builders<Station>.Filter.Empty)
            .ToListAsync(ct);

        public async Task<IEnumerable<Station>> GetStationsByRouteAsync(
            Route route,
            CancellationToken ct = default)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));
            var stationIds = route.Directions
                .SelectMany(d => new ObjectId[] { d.From, d.To })
                .Distinct();
            var filter = Builders<Station>.Filter.In(nameof(Station.StationId), stationIds);
            return await _stationsCollection
                .Find(filter)
                .ToListAsync(ct);
        }

        public async Task<Station> GetStationByIdAsync(ObjectId id, CancellationToken ct = default) =>
            await _stationsCollection
            .Find(s => s.StationId == id)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException();

        public async Task<IEnumerable<ObjectId>> GetExistingStationIdsAsync(
            IEnumerable<ObjectId> ids,
            CancellationToken ct = default) =>
            await _stationsCollection
                .Find(s => ids.Contains(s.StationId))
                .Project(s => s.StationId)
                .ToListAsync(ct);

        public async Task<Station> AddStationAsync(Station station, CancellationToken ct = default)
        { 
            await _stationsCollection.InsertOneAsync(station, null, ct);
            return station;
        }

        public async Task<Models.Enums.UpdateResult> UpdateStationAsync(
            ObjectId id,
            Station modifiedStation,
            CancellationToken ct = default)
        {
            var updateResult = await _stationsCollection.UpdateOneAsync(
                r => r.StationId == id,
                Builders<Station>.Update
                    .Set(nameof(Station.EstimatedWaitingTime), modifiedStation.EstimatedWaitingTime),
                new UpdateOptions { IsUpsert = false },
                ct);
            if (updateResult.MatchedCount < 1)
                return Models.Enums.UpdateResult.Failed;
            if (updateResult.ModifiedCount < 1)
                return Models.Enums.UpdateResult.Matched;
            return Models.Enums.UpdateResult.Matched | Models.Enums.UpdateResult.Modified;
        }

        public async Task<bool> DeleteOneAsync(ObjectId id, CancellationToken ct = default) =>
            (await _stationsCollection.DeleteOneAsync(r => r.StationId == id, ct)).DeletedCount > 0;
    }
}

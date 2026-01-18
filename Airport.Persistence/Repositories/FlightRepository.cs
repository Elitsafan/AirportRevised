using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Airport.Persistence.Repositories
{
    internal sealed class FlightRepository : IFlightRepository
    {
        #region Fields
        private readonly ILogger<FlightRepository> _logger;
        private readonly IMongoCollection<Flight> _flightsCollection;
        private readonly IMongoClient _client;
        #endregion

        public FlightRepository(
            ILogger<FlightRepository> logger,
            IMongoClient client,
            IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _logger = logger;
            _client = client;
            _flightsCollection = _client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Flight>(dbConfiguration.Value.FlightsCollectionName);
        }

        public async Task AddFlightAsync(Flight flight, CancellationToken ct = default) =>
            await _flightsCollection.InsertOneAsync(flight, cancellationToken: ct);

        public async Task<IEnumerable<Flight>> GetAllAsync(CancellationToken ct = default) => await _flightsCollection
            .Find(Builders<Flight>.Filter.Empty)
            .ToListAsync(ct);

        //public async Task<IEnumerable<T>> OfTypeAsync<T>(CancellationToken ct = default) where T : Flight => await _flightsCollection
        //    .OfType<T>()
        //    .Find(Builders<T>.Filter.Empty)
        //    .ToListAsync(ct);

        public async Task<IEnumerable<Flight>> OrderByEntranceAsync(CancellationToken ct = default) =>
            await _flightsCollection
                .Find(FilterDefinition<Flight>.Empty)
                .SortBy(f => f.OccupationDetails[0].Entrance)
                .ToListAsync(ct);

        public async Task<IEnumerable<Flight>> FilterByTimePassedAsync(
            TimeSpan timePassed,
            CancellationToken ct = default) => await _flightsCollection
            .Find(new FilterDefinitionBuilder<Flight>()
                .Gt(f => f.OccupationDetails[0].Entrance, DateTime.Now - timePassed))
            .SortBy(f => f.OccupationDetails[0].Entrance)
            .ToListAsync(ct);

        public async Task<Models.Enums.UpdateResult> UpdateFlightAsync(
            Flight flight,
            bool upsert = false,
            CancellationToken ct = default)
        {
            UpdateResult updateResult = await _flightsCollection.UpdateOneAsync(
                f => f.FlightId == flight.FlightId,
                Builders<Flight>.Update
                    .Set(nameof(Flight.OccupationDetails), flight.OccupationDetails)
                    .Set(nameof(Flight.RouteId), flight.RouteId),
                new UpdateOptions { IsUpsert = upsert },
                ct);
            if (updateResult.ModifiedCount > 0)
                return Models.Enums.UpdateResult.Modified;
            if (updateResult.MatchedCount > 0)
                return Models.Enums.UpdateResult.Matched;
            return Models.Enums.UpdateResult.Failed;
        }

        public async Task<Flight> GetFlightByIdAsync(ObjectId id, CancellationToken ct) =>
            await _flightsCollection
            .Find(f => f.FlightId == id)
            .SingleOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException();

        public async Task<bool> DeleteOneAsync(ObjectId id, CancellationToken ct = default) =>
            (await _flightsCollection.DeleteOneAsync(f => f.FlightId == id, ct)).DeletedCount > 0;
    }
}

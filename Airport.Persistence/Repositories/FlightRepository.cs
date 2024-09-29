using Airport.Contracts.Database;
using Airport.Contracts.Repositories;
using Airport.Models.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Airport.Persistence.Repositories
{
    internal sealed class FlightRepository : IFlightRepository
    {
        #region Fields
        private readonly ILogger<IFlightRepository> _logger;
        private readonly IMongoCollection<Flight> _flightsCollection;
        private readonly IMongoClient _client;
        #endregion

        public FlightRepository(
            ILogger<IFlightRepository> logger,
            IMongoClient client,
            IAirportDbConfiguration dbSettings)
        {
            _logger = logger;
            _client = client;
            _flightsCollection = _client
                .GetDatabase(dbSettings.DatabaseName)
                .GetCollection<Flight>(dbSettings.FlightsCollectionName);
        }

        public async Task AddFlightAsync(Flight flight, CancellationToken cancellationToken = default) =>
            await _flightsCollection.InsertOneAsync(flight, cancellationToken: cancellationToken);

        public async Task<IEnumerable<Flight>> GetAllAsync(CancellationToken cancellationToken = default) => await _flightsCollection
            .Find(Builders<Flight>.Filter.Empty)
            .ToListAsync(cancellationToken);

        //public async Task<IEnumerable<T>> OfTypeAsync<T>(CancellationToken cancellationToken = default) where T : Flight => await _flightsCollection
        //    .OfType<T>()
        //    .Find(Builders<T>.Filter.Empty)
        //    .ToListAsync(cancellationToken);

        public async Task<IEnumerable<Flight>> OrderByEntranceAsync(CancellationToken cancellationToken = default) =>
            await _flightsCollection
                .Find(FilterDefinition<Flight>.Empty)
                .SortBy(f => f.OccupationDetails[0].Entrance)
                .ToListAsync(cancellationToken);

        public async Task<IEnumerable<Flight>> FilterByTimePassedAsync(
            TimeSpan timePassed,
            CancellationToken cancellationToken = default) => await _flightsCollection
            .Find(new FilterDefinitionBuilder<Flight>()
                .Gt(f => f.OccupationDetails[0].Entrance, DateTime.Now - timePassed))
            .SortBy(f => f.OccupationDetails[0].Entrance)
            .ToListAsync(cancellationToken);

        public async Task<bool> UpdateFlightAsync(Flight flight, bool upsert = true, CancellationToken cancellationToken = default)
        {
            UpdateResult updateResult = await _flightsCollection.UpdateOneAsync(
                f => f.FlightId == flight.FlightId,
                Builders<Flight>.Update
                    .Set(nameof(Flight.OccupationDetails), flight.OccupationDetails)
                    .Set(nameof(Flight.RouteId), flight.RouteId),
                new UpdateOptions { IsUpsert = upsert },
                cancellationToken);
            return updateResult.MatchedCount > 0;
        }
    }
}

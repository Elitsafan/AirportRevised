using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Airport.Persistence.Repositories
{
    internal sealed class FlightRepository : IFlightRepository
    {
        private readonly IMongoCollection<Flight> _flightsCollection;

        public FlightRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration) => _flightsCollection = client
            .GetDatabase(dbConfiguration.Value.DatabaseName)
            .GetCollection<Flight>(dbConfiguration.Value.FlightsCollectionName);

        public async Task<IEnumerable<Flight>> GetAllAsync(CancellationToken ct = default) => await _flightsCollection
            .Find(Builders<Flight>.Filter.Empty)
            .ToListAsync(ct);

        public async Task<Flight> GetFlightByIdAsync(ObjectId id, CancellationToken ct) =>
            await _flightsCollection
            .Find(f => f.FlightId == id)
            .FirstOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException($"Flight with Id: {id} not found.");

        public async Task<Flight> AddOneAsync(Flight flight, CancellationToken ct = default)
        {
            await _flightsCollection.InsertOneAsync(flight, cancellationToken: ct);
            return flight;
        }

        public async Task<Models.Enums.UpdateResult> UpdateFlightAsync(
            Flight flight,
            bool upsert = false,
            CancellationToken ct = default)
        {
            UpdateResult updateResult = await _flightsCollection.UpdateOneAsync(
                f => f.FlightId == flight.FlightId,
                Builders<Flight>.Update
                    .Set(s => s.OccupationDetails, flight.OccupationDetails)
                    .Set(f => f.RouteId, flight.RouteId),
                new UpdateOptions { IsUpsert = upsert },
                ct);
            if (updateResult.ModifiedCount > 0)
                return Models.Enums.UpdateResult.Modified;
            if (updateResult.MatchedCount > 0)
                return Models.Enums.UpdateResult.Matched;
            return Models.Enums.UpdateResult.Failed;
        }

        public async Task<bool> DeleteOneAsync(ObjectId id, CancellationToken ct = default) =>
            (await _flightsCollection.DeleteOneAsync(f => f.FlightId == id, ct)).DeletedCount > 0;

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
    }
}
using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models;
using Airport.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Airport.Persistence.Repositories
{
    internal sealed class FlightRepository : IFlightRepository
    {
        #region Fields
        private readonly IMongoCollection<Flight> _flightsCollection;
        private readonly ConcurrentDictionary<ObjectId, Flight> _activeFlights;
        private readonly ConcurrentQueue<Flight> _completedFlights;
        #endregion

        public FlightRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _flightsCollection = client
            .GetDatabase(dbConfiguration.Value.DatabaseName)
            .GetCollection<Flight>(dbConfiguration.Value.FlightsCollectionName);
            _activeFlights = new();
            _completedFlights = new();
        }

        public async Task<IEnumerable<Flight>> GetAllAsync(CancellationToken ct = default) =>
            await _flightsCollection
                .Find(FilterDefinition<Flight>.Empty)
                .ToListAsync(ct);

        public async Task<Flight> GetByIdAsync(ObjectId id, CancellationToken ct) =>
            await _flightsCollection
                .Find(f => f.FlightId == id)
                .FirstOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException($"Flight Id: {id} not found.");

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

        public async Task<IPagedList<TResult>> GetPagedFlightsAsync<TResult>(
            Func<Flight, TResult> func,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
            where TResult : class
        {
            var totalCount = await _flightsCollection.CountDocumentsAsync(
                FilterDefinition<Flight>.Empty,
                cancellationToken: ct);
            var result = await _flightsCollection
                .Find(FilterDefinition<Flight>.Empty)
                .SortBy(f => f.OccupationDetails[0].Entrance)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(ct);

            if (result.Count == 0)
                return new PagedList<TResult>([], 0, 0, 0);
            if (pageSize * pageNumber > result.Count && pageNumber != Math.Ceiling((double)result.Count / pageSize))
                throw new InvalidOperationException("No such a page number for a such page size.");

            return new PagedList<TResult>(
                result.Select(func).ToList(),
                (int)totalCount,
                pageNumber,
                pageSize);
        }

        public async Task AddCompletedFlightAsync(Flight flight)
        {
            _completedFlights.Enqueue(flight);
            _activeFlights.TryRemove(flight.FlightId, out _);
            await Task.CompletedTask;
        }

        public async Task FlushAsync(CancellationToken ct = default) =>
            await _flightsCollection.InsertManyAsync(_completedFlights, cancellationToken: ct);

        //public Task<int> EnforceStorageLimitAsync(CancellationToken ct = default)
        //{

        //}
    }
}

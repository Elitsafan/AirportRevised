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
        private readonly AirportDbConfiguration _dbConfiguration;
        #endregion

        public FlightRepository(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            _flightsCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Flight>(dbConfiguration.Value.FlightsCollectionName);

            _dbConfiguration = dbConfiguration.Value;
            _activeFlights = new();
            _completedFlights = new();
        }

        public async Task<IEnumerable<Flight>> GetAllAsync(CancellationToken ct = default)
        {
            var dbFlights = await _flightsCollection
                .Find(FilterDefinition<Flight>.Empty)
                .ToListAsync(ct);

            return _activeFlights.Values.Concat(
                _completedFlights.Concat(
                    dbFlights))
                .ToList();
        }

        public async Task<Flight> GetByIdAsync(ObjectId id, CancellationToken ct)
        {
            var flight = _completedFlights.FirstOrDefault(f => f.FlightId == id);
            if (flight is not null)
                return flight;

            if (_activeFlights.TryGetValue(id, out flight))
                return flight;

            return await _flightsCollection
                .Find(f => f.FlightId == id)
                .FirstOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException($"Flight Id: {id} not found.");
        }

        public async Task<Flight> AddOneAsync(Flight flight, CancellationToken ct = default)
        {
            if (!_activeFlights.TryAdd(flight.FlightId, flight))
                throw new InvalidOperationException($"Unknown error while adding flight {flight.FlightId}.");
            return await Task.FromResult(flight);
        }

        public async Task<Models.Enums.UpdateResult> UpdateFlightAsync(
            Flight flight,
            bool upsert = false,
            CancellationToken ct = default)
        {
            try
            {
                if (upsert)
                {
                    _activeFlights.AddOrUpdate(
                        flight.FlightId,
                        id => flight,
                        (id, flightToModify) =>
                        {
                            flightToModify.OccupationDetails = flight.OccupationDetails;
                            flightToModify.RouteId = flight.RouteId;
                            return flightToModify;
                        });
                    return await Task.FromResult(Models.Enums.UpdateResult.Modified);
                }
                if (_activeFlights.TryGetValue(flight.FlightId, out var currentFlight))
                {
                    if (_activeFlights.TryUpdate(flight.FlightId, flight, currentFlight))
                        return await Task.FromResult(Models.Enums.UpdateResult.Modified);
                    return await Task.FromResult(Models.Enums.UpdateResult.Matched);
                }
                return await Task.FromResult(Models.Enums.UpdateResult.Failed);
            }
            catch (Exception)
            {
                return await Task.FromResult(Models.Enums.UpdateResult.Failed);
            }
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
            if (!_activeFlights.TryRemove(flight.FlightId, out _))
                throw new InvalidOperationException($"Error while handling completed flight {flight.FlightId}.");
            await Task.CompletedTask;
        }

        public async Task<long> FlushAsync(CancellationToken ct = default)
        {
            var writeList = new List<WriteModel<Flight>>(_dbConfiguration.FlightSaveBatchSize);
            while (writeList.Count < _dbConfiguration.FlightSaveBatchSize &&
                 _completedFlights.TryDequeue(out var flight))
                writeList.Add(new InsertOneModel<Flight>(flight));

            if (writeList.Count == 0)
                return 0;

            var result = await _flightsCollection.BulkWriteAsync(writeList, cancellationToken: ct);

            return result.InsertedCount;
        }

        public async Task<long> EnforceStorageLimitAsync(CancellationToken ct = default)
        {
            var totalCount = await _flightsCollection
                .CountDocumentsAsync(FilterDefinition<Flight>.Empty, cancellationToken: ct);

            if (totalCount <= _dbConfiguration.MaxFlightDocuments)
                return 0;

            var lastToDelete = await _flightsCollection
                .Find(FilterDefinition<Flight>.Empty)
                .SortBy(f => f.OccupationDetails[0].Entrance)
                .Skip((int)(totalCount - _dbConfiguration.MaxFlightDocuments))
                .FirstAsync();

            var result = await _flightsCollection.DeleteManyAsync(
                f => f.OccupationDetails[0].Entrance < lastToDelete.OccupationDetails[0].Entrance,
                ct);

            return result.DeletedCount;
        }
    }
}

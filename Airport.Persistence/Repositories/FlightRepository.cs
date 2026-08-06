using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models;
using MongoDB.Driver.Linq;
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
                .AsQueryable(new AggregateOptions { AllowDiskUse = true })
                .ToListAsync(ct);

            return _activeFlights.Values.Concat(
                _completedFlights.Concat(
                    dbFlights))
                .ToList();
        }

        public async Task<Flight> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            var flight = _completedFlights.FirstOrDefault(f => f.FlightId == id);

            if (flight is not null)
                return flight;

            if (_activeFlights.TryGetValue(id, out flight))
                return flight;

            return await _flightsCollection.AsQueryable(new AggregateOptions { AllowDiskUse = true })
                .FirstOrDefaultAsync(f => f.FlightId == id, ct)
                ?? throw new EntityNotFoundException($"Flight Id: {id} not found.");
        }

        public async Task<Flight> AddOneAsync(Flight flight, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (!_activeFlights.TryAdd(flight.FlightId, flight))
                throw new InvalidOperationException($"Unknown error while adding flight {flight.FlightId}.");

            return await Task.FromResult(flight);
        }

        public async Task<bool> DeleteOneAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default) => session is null
            ? (await _flightsCollection.DeleteOneAsync(f => f.FlightId == id, cancellationToken: ct)).DeletedCount > 0
            : (await _flightsCollection.DeleteOneAsync(session, f => f.FlightId == id, null, ct)).DeletedCount > 0;

        public async Task<IEnumerable<Flight>> FilterByTimePassedAsync(TimeSpan timePassed, CancellationToken ct = default)
        {
            var qActive = _activeFlights.Values
                .Where(f => f.OccupationDetails[0].Entrance > DateTime.Now - timePassed)
                .OrderBy(f => f.OccupationDetails[0].Entrance);

            var result = new List<Flight>(qActive);

            var qCompleted = _completedFlights
                .Where(f => f.OccupationDetails[0].Entrance > DateTime.Now - timePassed);

            result.AddRange(qCompleted);

            result = result.OrderBy(f => f.OccupationDetails[0].Entrance).ToList();

            var dbresult = await _flightsCollection
                .Find(new FilterDefinitionBuilder<Flight>()
                .Gt(f => f.OccupationDetails[0].Entrance, DateTime.Now - timePassed), new FindOptions { AllowDiskUse = true })
                .SortBy(f => f.OccupationDetails[0].Entrance)
                .ToListAsync(ct);

            if (dbresult.Count == 0)
                return result;

            result.AddRange(dbresult);

            return result.OrderBy(f => f.OccupationDetails[0].Entrance).ToList();
        }

        public async Task<IPagedList<TResult>> GetPagedFlightsAsync<TResult>(
            Func<Flight, TResult> func,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
            where TResult : class
        {
            var dbCount = await _flightsCollection
                .AsQueryable(new AggregateOptions { AllowDiskUse = true })
                .CountAsync(ct);

            var totalCount = dbCount +
                _activeFlights.Count +
                _completedFlights.Count;

            var skipCount = (pageNumber - 1) * pageSize;

            var result = new List<Flight>(pageSize);

            if (skipCount < dbCount)
                result.AddRange(await _flightsCollection
                    .AsQueryable(new AggregateOptions { AllowDiskUse = true })
                    .OrderBy(f => f.OccupationDetails[0].Entrance)
                    .Skip(skipCount)
                    .Take(pageSize)
                    .ToListAsync(ct));

            if (result.Count < pageSize)
            {
                var remaining = pageSize - result.Count;

                var memSkip = skipCount < dbCount ? 0 : skipCount - dbCount;

                result.AddRange(_completedFlights
                    .Concat(_activeFlights.Values)
                    .OrderBy(f => f.OccupationDetails[0].Entrance)
                    .Skip(memSkip)
                    .Take(remaining));
            }

            if (pageSize * pageNumber > result.Count && pageNumber > Math.Ceiling((double)totalCount / pageSize))
                throw new InvalidOperationException("No such a page number for a such page size.");

            if (result.Count == 0)
                return new PagedList<TResult>([], 0, 0, 0);

            return new PagedList<TResult>(
                result.Select(func).ToList(),
                totalCount,
                pageNumber,
                pageSize);
        }

        public void AddCompletedFlight(Flight flight)
        {
            _completedFlights.Enqueue(flight);

            if (!_activeFlights.TryRemove(flight.FlightId, out _))
                throw new InvalidOperationException($"Error while handling completed flight {flight.FlightId}.");
        }

        public async Task<long> FlushAsync(IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            var writeList = new List<WriteModel<Flight>>(_dbConfiguration.FlightSaveBatchSize);

            while (writeList.Count < _dbConfiguration.FlightSaveBatchSize && _completedFlights.TryDequeue(out var flight))
                writeList.Add(new InsertOneModel<Flight>(flight));

            if (writeList.Count == 0)
                return 0;

            var result = session is null
                ? await _flightsCollection.BulkWriteAsync(writeList, cancellationToken: ct)
                : await _flightsCollection.BulkWriteAsync(session, writeList, null, ct);

            return result.InsertedCount;
        }

        public async Task<long> EnforceStorageLimitAsync(IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            var totalCount = await _flightsCollection
                .AsQueryable(new AggregateOptions { AllowDiskUse = true })
                .CountAsync(ct);

            if (totalCount <= _dbConfiguration.MaxFlightDocuments)
                return 0;

            var lastToDelete = await _flightsCollection
                .AsQueryable(new AggregateOptions { AllowDiskUse = true })
                .OrderBy(f => f.OccupationDetails[0].Entrance)
                .Skip(totalCount - _dbConfiguration.MaxFlightDocuments)
                .FirstAsync(ct);

            var result = session is null
                ? await _flightsCollection.DeleteManyAsync(
                    f => f.OccupationDetails[0].Entrance < lastToDelete.OccupationDetails[0].Entrance,
                    cancellationToken: ct)
                : await _flightsCollection.DeleteManyAsync(
                    session,
                    f => f.OccupationDetails[0].Entrance < lastToDelete.OccupationDetails[0].Entrance,
                    null,
                    ct);

            return result.DeletedCount;
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
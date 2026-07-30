using Airport.Services.Abstractions;
using Airport.Services.Extensions;
using System.Runtime.CompilerServices;

namespace Airport.Services.Services
{
    public class FlightService : IFlightService
    {
        #region Fields
        private readonly IAirportStateProvider _airportStateProvider;
        private readonly IRepositoryManager _repoManager;
        private readonly IMapper _mapper;
        private readonly IFlightQueue _queue;
        private readonly ILogger<FlightService> _logger;
        #endregion

        public FlightService(
            IAirportStateProvider airportStateProvider,
            IRepositoryManager repoManager,
            IMapper mapper,
            IFlightQueue queue,
            ILogger<FlightService> logger)
        {
            _airportStateProvider = airportStateProvider;
            _repoManager = repoManager;
            _mapper = mapper;
            _queue = queue;
            _logger = logger;
        }

        public async Task<FlightDTO> AddFlightAsync(
            FlightForCreationDTO flightToCreate,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (flightToCreate is null)
                throw new ArgumentNullException(nameof(flightToCreate));

            Flight flight = _mapper.Map<Flight>(flightToCreate);

            flight.FlightId = ObjectId.GenerateNewId();

            await _queue.AddFlightAsync(flight);

            return _mapper.Map<FlightDTO>(flight);
        }

        public async IAsyncEnumerable<FlightDTO> GetAllFlightsAsync(
            int? minutesPassed,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var flights = minutesPassed.HasValue
                ? await _repoManager.FlightRepository.FilterByTimePassedAsync(
                    TimeSpan.FromMinutes(minutesPassed.Value),
                    ct)
                : await _repoManager.FlightRepository.GetAllAsync(ct);

            foreach (var flight in flights.Select(_mapper.Map<FlightDTO>))
                yield return flight;
        }

        public async Task<FlightDTO> GetFlightByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var flight = await _repoManager.FlightRepository.GetByIdAsync(id, ct);

            return _mapper.Map<FlightDTO>(flight);
        }

        public async Task<bool> DeleteFlightAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var result = await _repoManager.FlightRepository.DeleteOneAsync(id, ct: ct);

            if (!result)
                _logger.LogInformation("Flight with id: {id} not found", id);

            return result;
        }
    }
}

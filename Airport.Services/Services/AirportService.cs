using Airport.Domain.Helpers;
using Airport.Models;
using Airport.Models.Enums;
using Airport.Services.Abstractions;
using Airport.Services.Extensions;
using MongoDB.Driver;

namespace Airport.Services.Services
{
    public sealed class AirportService : IAirportService
    {
        #region Fields
        private readonly IMapper _mapper;
        private readonly ILogger<AirportService> _logger;
        private readonly IRepositoryManager _repoManager;
        private readonly IDomainEvents _domainEvents;
        private readonly IAirportStateProvider _airportStateProvider;
        #endregion

        public AirportService(
            IAirportStateProvider airportStateProvider,
            IRepositoryManager repoManager,
            IDomainEvents domainEvents,
            IMapper mapper,
            ILogger<AirportService> logger)
        {
            _airportStateProvider = airportStateProvider;
            _repoManager = repoManager;
            _domainEvents = domainEvents;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<string> StartAsync(CancellationToken ct = default)
        {
            if (_airportStateProvider.HasStarted)
                return "Airport already started.";

            using var releaser = await _airportStateProvider.StartLock.EnterAsync(ct);

            if (_airportStateProvider.HasStarted)
                return "Airport already started.";

            _airportStateProvider.HasStarted = true;

            _logger.LogInformation("Airport started.");

            return "Airport Started.";
        }

        public async Task<string> RestartAsync(CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            await _domainEvents.RaiseSystemResetRequestedAsync();

            _logger.LogInformation("Airport restarted.");

            return "Airport restarted.";
        }

        public async Task<IAirportStatus> GetStatusAsync(CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            List<StationDTO> stations = (await _repoManager.StationRepository.GetAllAsync(ct))
                .Select(_mapper.Map<StationDTO>)
                .ToList();

            List<RouteDTO> routes = (await _repoManager.RouteRepository.GetAllAsync(ct))
                .Select(_mapper.Map<RouteDTO>)
                .ToList();

            return new AirportStatus
            {
                Stations = stations,
                Routes = routes
            };
        }

        public async Task<SummaryWithMetadata> GetSummaryWithMetadataAsync(
            GetSummaryParameters parameters,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            var summary = await GetPagedSummaryAsync(parameters, ct);

            return new SummaryWithMetadata
            {
                Summary = summary,
                LandingsCount = summary.Count(f => f.FlightType == FlightType.Landing),
                DeparturesCount = summary.Count(f => f.FlightType == FlightType.Departure)
            };
        }

        private async Task<IPagedList<FlightSummary>> GetPagedSummaryAsync(
            GetSummaryParameters parameters,
            CancellationToken ct = default) => await _repoManager.FlightRepository.GetPagedFlightsAsync(
            f => new FlightSummary
            {
                Stations = f.OccupationDetails,
                FlightId = f.FlightId,
                FlightType = f.ToFlightType()
            },
            parameters.PageNumber,
            parameters.PageSize,
            ct);
    }
}

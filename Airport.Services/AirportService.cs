using Airport.Contracts.Providers;
using Airport.Domain.Providers;
using Airport.Domain.Repositories;
using Airport.Models;
using Airport.Models.DTOs;
using Airport.Models.Entities;
using Airport.Services.Abstractions;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using MongoDB.Driver;

namespace Airport.Services
{
    public sealed class AirportService : IAirportService
    {
        #region Fields
        private readonly IMapper _mapper;
        private readonly ILogger<AirportService> _logger;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IStationLogicProvider _stationLogicProvider;
        private readonly IAirportStateProvider _airportStateProvider;
        #endregion

        public AirportService(
            IAirportStateProvider airportStateProvider,
            IStationLogicProvider stationLogicProvider,
            IRepositoryManager repositoryManager,
            IMapper mapper,
            ILogger<AirportService> logger)
        {
            _airportStateProvider = airportStateProvider;
            _stationLogicProvider = stationLogicProvider ?? throw new ArgumentNullException(nameof(stationLogicProvider));
            _repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> StartAsync(CancellationToken ct = default)
        {
            using var releaser = await _airportStateProvider.StartLock.EnterAsync(ct);

            if (_airportStateProvider.HasStarted)
                return "Already started";

            _logger.LogInformation("Airport started.");
            _airportStateProvider.HasStarted = true;

            return "Started";
        }

        public async Task<IAirportStatus> GetStatusAsync(CancellationToken ct = default)
        {
            List<StationDTO> stations = (await _stationLogicProvider.GetAllAsync(ct))
                .Select(_mapper.Map<StationDTO>)
                .ToList();
            List<RouteDTO> routes = (await _repositoryManager.RouteRepository.GetAllAsync(ct))
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
            var summary = await GetPagedSummaryAsync(parameters, ct);
            var (landings, departures) = await GetFlightsCountAsync(summary.ItemsProcessed, ct);
            return new SummaryWithMetadata
            {
                Summary = summary,
                LandingsCount = landings,
                DeparturesCount = departures
            };
        }

        public async ValueTask DisposeAsync() => await _repositoryManager.DisposeAsync();

        private async Task<IPagedList<FlightSummary>> GetPagedSummaryAsync(
            GetSummaryParameters parameters,
            CancellationToken ct = default) => (await _repositoryManager.FlightRepository
                .OrderByEntranceAsync(ct))
                .Select(f => new FlightSummary
                {
                    FlightId = f.FlightId,
                    Stations = f.OccupationDetails,
                    FlightType = f.ConvertToFlightType()
                })
                .ToPagedList(parameters.PageNumber, parameters.PageSize);

        private async Task<(int LandingsCount, int DeparturesCount)> GetFlightsCountAsync(
            int count,
            CancellationToken ct = default)
        {
            var flights = await _repositoryManager.FlightRepository
                .OrderByEntranceAsync(ct);
            return (
                flights
                    .Take(count)
                    .OfType<Landing>()
                    .Count(),
                flights
                    .Take(count)
                    .OfType<Departure>()
                    .Count());
        }
    }
}

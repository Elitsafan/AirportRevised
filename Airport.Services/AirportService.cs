using Airport.Contracts.Providers;
using Airport.Domain.Helpers;
using Airport.Domain.Repositories;
using Airport.Models;
using Airport.Models.DTOs;
using Airport.Models.Enums;
using Airport.Services.Abstractions;
using Airport.Services.Extensions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Airport.Services
{
    public sealed class AirportService : IAirportService
    {
        #region Fields
        private readonly IMapper _mapper;
        private readonly ILogger<AirportService> _logger;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IAirportStateProvider _airportStateProvider;
        #endregion

        public AirportService(
            IAirportStateProvider airportStateProvider,
            IRepositoryManager repositoryManager,
            IMapper mapper,
            ILogger<AirportService> logger)
        {
            _airportStateProvider = airportStateProvider;
            _repositoryManager = repositoryManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<string> StartAsync(CancellationToken ct = default)
        {
            if (_airportStateProvider.HasStarted)
                return "Already started";

            using var releaser = await _airportStateProvider.StartLock.EnterAsync(ct);

            if (_airportStateProvider.HasStarted)
                return "Already started";

            _logger.LogInformation("Airport started.");
            _airportStateProvider.HasStarted = true;

            return "Started";
        }

        public async Task<IAirportStatus> GetStatusAsync(CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            List<StationDTO> stations = (await _repositoryManager.StationRepository.GetAllAsync(ct))
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

        public async ValueTask DisposeAsync() => await _repositoryManager.DisposeAsync();

        private async Task<IPagedList<FlightSummary>> GetPagedSummaryAsync(
            GetSummaryParameters parameters,
            CancellationToken ct = default) => await _repositoryManager.FlightRepository
            .GetPagedFlightsAsync(
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

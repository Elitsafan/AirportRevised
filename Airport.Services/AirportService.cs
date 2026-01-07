using Airport.Contracts.Factories;
using Airport.Contracts.Providers;
using Airport.Domain.Repositories;
using Airport.Models;
using Airport.Models.DTOs;
using Airport.Models.Entities;
using Airport.Services.Abstractions;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using MongoDB.Driver;

namespace Airport.Services
{
    public sealed class AirportService : IAirportService
    {
        #region Fields
        private static readonly AsyncSemaphore _semaphore;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AirportService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IStationLogicProvider _stationLogicProvider;
        #endregion

        static AirportService() => _semaphore = new(1);

        public AirportService(
            IServiceProvider serviceProvider,
            IStationLogicProvider stationLogicProvider,
            IRepositoryManager repositoryManager,
            IMapper mapper,
            IMemoryCache cache,
            ILogger<AirportService> logger)
        {
            _serviceProvider = serviceProvider;
            _stationLogicProvider = stationLogicProvider ?? throw new ArgumentNullException(nameof(stationLogicProvider));
            _repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool HasStarted => _cache.TryGetValue(nameof(HasStarted), out bool hasStarted) && hasStarted;

        public async Task<string> StartAsync(CancellationToken cancellationToken = default)
        {
            var releaser = await _semaphore.EnterAsync(cancellationToken);
            try
            {
                if (HasStarted)
                    return "Already started";

                InstantiateServices();
                SetHasStarted();

                _logger.LogInformation("Airport started.");
                return "Started";
            }
            finally { releaser.Dispose(); }
        }

        public async Task<IAirportStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            List<StationDTO> stations = (await _stationLogicProvider.GetAllAsync(cancellationToken))
                .Select(_mapper.Map<StationDTO>)
                .ToList();
            List<RouteDTO> routes = (await _repositoryManager.RouteRepository.GetAllAsync(cancellationToken))
                .Select(_mapper.Map<RouteDTO>)
                .ToList();

            return new AirportStatus
            {
                Stations = stations,
                Routes = routes
            };
        }

        public async Task<IPagedList<FlightSummary>> GetPagedSummaryAsync(
            GetSummaryParameters parameters,
            CancellationToken cancellationToken = default) => (await _repositoryManager.FlightRepository
                .OrderByEntranceAsync(cancellationToken))
                .Select(f => new FlightSummary
                {
                    FlightId = f.FlightId,
                    Stations = f.OccupationDetails,
                    FlightType = f.ConvertToFlightType()
                })
                .ToPagedList(parameters.PageNumber, parameters.PageSize);

        public async Task<(int LandingsCount, int DeparturesCount)> GetFlightsCountAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            var flights = await _repositoryManager.FlightRepository
                .OrderByEntranceAsync(cancellationToken);
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

        public async ValueTask DisposeAsync() => await _repositoryManager.DisposeAsync();

        private void InstantiateServices()
        {
            _serviceProvider.GetRequiredService<IAirportHubService>();
            _serviceProvider.GetRequiredService<IFlightLogicFactory>();
            _serviceProvider.GetRequiredService<IDirectionLogicFactory>();
            _serviceProvider.GetRequiredService<IStationLogicFactory>();
            _serviceProvider.GetRequiredService<IRouteLogicFactory>();
            _serviceProvider.GetRequiredService<IDirectionLogicProvider>();
            _serviceProvider.GetRequiredService<IStationLogicProvider>();
            _serviceProvider.GetRequiredService<IRouteLogicProvider>();
            _serviceProvider.GetRequiredService<IMongoClient>();
        }

        private void SetHasStarted()
        {
            var entryOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.NeverRemove)
                .SetSize(1);
            _cache.Set(nameof(HasStarted), true, entryOptions);
        }
    }
}

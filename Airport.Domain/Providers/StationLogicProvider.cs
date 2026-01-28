using Airport.Domain.EventArgs;
using Airport.Domain.Repositories;
using Airport.Models.Enums;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Airport.Domain.Providers
{
    public class StationLogicProvider : IStationLogicProvider
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly IStationLogicFactory _stationLogicFactory;
        private readonly IMemoryCache _cache;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<StationLogicProvider> _logger;
        private readonly ConcurrentDictionary<ObjectId, IStationLogic> _stationLogics;
        private readonly AsyncSemaphore _initializationSemaphore;
        private bool _isInitialized;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShortCacheExpiration = TimeSpan.FromMinutes(5);

        private const string ALL_STATIONS_KEY = "all_station_logics";
        private const string ROUTE_STATIONS_PREFIX = "route_stations_";
        private const string ROUTE_TRAFFIC_LIGHTS_PREFIX = "route_traffic_lights_";
        private const string NEXT_TRAFFIC_LIGHTS_PREFIX = "next_traffic_lights_";
        #endregion

        public StationLogicProvider(
            IServiceProvider serviceProvider,
            IStationLogicFactory stationLogicFactory,
            IMemoryCache cache,
            IDomainEvents domainEvents,
            ILogger<StationLogicProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _stationLogicFactory = stationLogicFactory;
            _cache = cache;
            _domainEvents = domainEvents;
            _logger = logger;
            _stationLogics = new ConcurrentDictionary<ObjectId, IStationLogic>();
            _initializationSemaphore = new(1);

            _domainEvents.StationCreated += OnStationCreatedAsync;
            _domainEvents.StationDeleted += OnStationDeletedAsync;
            _domainEvents.StationUpdated += OnStationUpdatedAsync;
            _domainEvents.DataRefreshed += OnDataRefreshedAsync;
            _domainEvents.SystemResetRequested += OnSystemResetRequestedAsync;
        }

        public event AsyncEventHandler<IStationChangedEventArgs<IStationChangedData>>? AnyStationOccupied;
        public event AsyncEventHandler<IStationChangedEventArgs<IStationChangedData>>? AnyStationCleared;

        public async Task<IEnumerable<IStationLogic>> GetAllAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(
                ALL_STATIONS_KEY,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                    entry.Size = 1;

                    _logger.LogDebug($"Caching all station logics ({_stationLogics.Count} items)");
                    return _stationLogics.Values;
                }) ?? [];
        }

        public async Task<IStationLogic> GetStationLogicByIdAsync(
            ObjectId id,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            if (_stationLogics.TryGetValue(id, out var stationLogic))
                return stationLogic;

            throw new LogicNotFoundException($"Station logic not found for Id: {id}");
        }

        public async Task<IEnumerable<IStationLogic>> FindStationLogicsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var cacheKey = $"{ROUTE_STATIONS_PREFIX}{routeId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                entry.Size = 1;

                await using var repositoryManager = _serviceProvider
                    .CreateAsyncScope()
                    .ServiceProvider
                    .GetRequiredService<IRepositoryManager>();

                IEnumerable<Station> stations;
                Route route;

                try
                {
                    route = await repositoryManager.RouteRepository.GetRouteByIdAsync(routeId, ct);
                    stations = await repositoryManager.StationRepository.GetStationsByRouteAsync(route, ct);
                }
                catch (EntityNotFoundException e)
                {
                    throw new LogicProvisionFailedException($"Route with id: {routeId} not found.", e);
                }

                var result = stations.Join(
                    _stationLogics.Values,
                    s => s.StationId,
                    sl => sl.StationId,
                    (station, stationLogic) => stationLogic)
                    .ToList();

                _logger.LogDebug($"Cached {result.Count} station logics for route {routeId}");
                return result;
            }) ?? [];
        }

        public async Task<IEnumerable<IStationLogic>> FindTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var cacheKey = $"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{routeId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                entry.Size = 1;

                await using var repositoryManager = _serviceProvider
                    .CreateAsyncScope()
                    .ServiceProvider
                    .GetRequiredService<IRepositoryManager>();

                var trafficLights = await repositoryManager.TrafficLightRepository
                    .GetTrafficLightsByRouteIdAsync(routeId, ct);

                var result = _stationLogics.Values.Join(
                    trafficLights,
                    s => s.StationId,
                    tl => tl.StationId,
                    (stationLogic, trafficLight) => stationLogic)
                    .ToList();

                _logger.LogDebug($"Cached {result.Count} traffic light logics for route {routeId}");
                return result;
            }) ?? [];
        }

        public async Task<IEnumerable<IStationLogic>> FindNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId trafficLightId,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var cacheKey = $"{NEXT_TRAFFIC_LIGHTS_PREFIX}{routeId}_{trafficLightId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ShortCacheExpiration; // Shorter cache for dynamic data
                entry.Size = 1;

                try
                {
                    await using var repositoryManager = _serviceProvider
                        .CreateAsyncScope()
                        .ServiceProvider
                        .GetRequiredService<IRepositoryManager>();

                    var nextTrafficLights = await repositoryManager.TrafficLightRepository
                        .GetNextTrafficLightsAsync(routeId, trafficLightId, ct);

                    var result = _stationLogics.Values.Join(
                        nextTrafficLights,
                        s => s.StationId,
                        tl => tl.StationId,
                        (stationLogic, trafficLight) => stationLogic)
                        .ToList();

                    _logger.LogDebug($"Cached {result.Count} next traffic light logics " +
                        $"for route {routeId}, traffic light {trafficLightId}");
                    return result;
                }
                catch (EntityNotFoundException ex)
                {
                    _logger.LogError(ex, $"Route not found when getting next traffic lights: {routeId}");
                    throw new InvalidOperationException($"Route not found. Cannot get next traffic lights for route: {routeId}", ex);
                }
            }) ?? [];
        }

        public void Dispose()
        {
            _initializationSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_isInitialized)
                return;

            using var releaser = await _initializationSemaphore.EnterAsync(ct);
            if (_isInitialized)
                return;

            await InitializeAsync(ct);
            _isInitialized = true;
        }

        private async Task InitializeAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Initializing station logics from repository");

            await using var scope = _serviceProvider.CreateAsyncScope();
            var repositoryManager = scope.ServiceProvider.GetRequiredService<IRepositoryManager>();

            var stations = await repositoryManager.StationRepository.GetAllAsync(ct);
            if (!stations.Any())
            {
                _logger.LogError("No stations found during initialization");
                throw new InvalidOperationException("There are no stations.");
            }

            var stationLogics = stations.Select(station => _stationLogicFactory
                .GetCreator(station)
                .Create());

            foreach (var stationLogic in stationLogics)
            {
                stationLogic.StationOccupiedAsync += async (s, e) =>
                {
                    if (e.StationLogic.StationId == stationLogic.StationId)
                        await OnInternalStationOccupiedAsync(s, e);
                };
                stationLogic.StationClearedAsync += async (s, e) =>
                {
                    if (e.StationLogic.StationId == stationLogic.StationId)
                        await OnInternalStationClearedAsync(s, e);
                };
                _stationLogics.TryAdd(stationLogic.StationId, stationLogic);
            }

            _logger.LogInformation($"Successfully initialized {_stationLogics.Count} station logics");
        }

        #region Handlers
        private async Task OnDataRefreshedAsync() => await RefreshAsync();

        private async Task OnSystemResetRequestedAsync() => await RefreshAsync();

        private async Task OnStationCreatedAsync(object? sender, IStationCreatedEventArgs args)
        {
            var releaser = await _initializationSemaphore.EnterAsync();
            try
            {
                InvalidateCache();
            }
            finally { releaser.Dispose(); }
        }

        private async Task OnStationUpdatedAsync(object? sender, IStationUpdatedEventArgs args)
        {
            var releaser = await _initializationSemaphore.EnterAsync();
            try
            {
                InvalidateCache();
            }
            finally { releaser.Dispose(); }
        }

        private async Task OnStationDeletedAsync(object? sender, IStationDeletedEventArgs args)
        {
            var releaser = await _initializationSemaphore.EnterAsync();
            try
            {
                InvalidateCache();
            }
            finally { releaser.Dispose(); }
        } 
        #endregion

        private async Task OnInternalStationOccupiedAsync(object? sender, IStationOccupiedEventArgs args)
        {
            var changedData = new StationChangedEventArgs { StationsState = PopulateStationChangedQuery() };
            await (AnyStationOccupied?.InvokeAsync(sender, changedData) ?? Task.CompletedTask);
        }

        private async Task OnInternalStationClearedAsync(object? sender, IStationClearedEventArgs args)
        {
            var changedData = new StationChangedEventArgs { StationsState = PopulateStationChangedQuery() };
            await (AnyStationCleared?.InvokeAsync(sender, changedData) ?? Task.CompletedTask);
        }

        // Prepare stations query for sending the state of stations
        private IEnumerable<IStationChangedData> PopulateStationChangedQuery() =>
            _stationLogics
                .OrderBy(s => s.Value.StationId)
                .Select(s => new StationChangedData
                {
                    StationId = s.Value.StationId,
                    Flight = s.Value.CurrentFlightId is null
                        ? null
                        : new FlightInfo
                        {
                            FlightId = s.Value.CurrentFlightId,
                            FlightType = s.Value.CurrentFlightType
                        },
                });

        #region Data Query Helpers
        private class StationChangedData : IStationChangedData
        {
            public ObjectId StationId { get; init; }
            public IFlightInfo? Flight { get; init; }
        }

        private class FlightInfo : IFlightInfo
        {
            public ObjectId? FlightId { get; init; }
            public FlightType? FlightType { get; init; }
        } 
        #endregion

        private async Task RefreshAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Refreshing station logics and clearing cache");

            using var releaser = await _initializationSemaphore.EnterAsync(ct);
            // Clear cache first
            InvalidateCache();

            // Clear existing station logics
            _stationLogics.Clear();

            // Re-initialize
            _isInitialized = false;
            await InitializeAsync(ct);
            _isInitialized = true;

            _logger.LogInformation("Station logics refreshed successfully");
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all cache entries");

            // Remove the main cache entry
            _cache.Remove(ALL_STATIONS_KEY);
        }
    }
}

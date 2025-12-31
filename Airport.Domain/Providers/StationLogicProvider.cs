using Airport.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Airport.Domain.Providers
{
    public class StationLogicProvider : IStationLogicProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<StationLogicProvider> _logger;
        private readonly ConcurrentDictionary<ObjectId, IStationLogic> _stationLogics;
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private bool _isInitialized = false;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShortCacheExpiration = TimeSpan.FromMinutes(5);

        private const string ALL_STATIONS_KEY = "all_station_logics";
        private const string ROUTE_STATIONS_PREFIX = "route_stations_";
        private const string ROUTE_TRAFFIC_LIGHTS_PREFIX = "route_traffic_lights_";
        private const string NEXT_TRAFFIC_LIGHTS_PREFIX = "next_traffic_lights_";

        private StationLogicProvider(
            IServiceProvider serviceProvider, 
            IMemoryCache cache, 
            ILogger<StationLogicProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _logger = logger;
            _stationLogics = new ConcurrentDictionary<ObjectId, IStationLogic>();
        }

        public static async Task<StationLogicProvider> CreateAsync(
            IServiceProvider serviceProvider,
            IMemoryCache cache,
            ILogger<StationLogicProvider> logger)
        {
            var provider = new StationLogicProvider(serviceProvider, cache, logger);
            await provider.InitializeAsync();
            return provider;
        }

        public async Task<IEnumerable<IStationLogic>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);

            return await _cache.GetOrCreateAsync(
                ALL_STATIONS_KEY, 
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                    entry.Size = 1; // For memory cache size tracking

                    _logger.LogDebug("Caching all station logics ({Count} items)", _stationLogics.Count);
                    return await Task.FromResult(_stationLogics.Values.ToList().AsEnumerable());
                }) ?? [];
        }

        public async Task<IStationLogic> GetStationLogicByIdAsync(
            ObjectId id, 
            CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);

            if (_stationLogics.TryGetValue(id, out var stationLogic))
                return stationLogic;

            _logger.LogWarning("Station logic not found for ID: {StationId}", id);
            throw new LogicNotFoundException($"Station logic not found for ID: {id}");
        }

        public async Task<IEnumerable<IStationLogic>> FindStationLogicsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);

            var cacheKey = $"{ROUTE_STATIONS_PREFIX}{routeId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                entry.Size = 1;

                try
                {
                    await using var repositoryManager = _serviceProvider
                        .CreateAsyncScope()
                        .ServiceProvider
                        .GetRequiredService<IRepositoryManager>();

                    var route = await repositoryManager.RouteRepository.GetByIdAsync(routeId, cancellationToken);
                    var stations = await repositoryManager.StationRepository.GetStationsByRouteAsync(route, cancellationToken);

                    var result = stations.Join(
                        _stationLogics.Values,
                        s => s.StationId,
                        sl => sl.StationId,
                        (station, stationLogic) => stationLogic).ToList();

                    _logger.LogDebug("Cached {Count} station logics for route {RouteId}", result.Count, routeId);
                    return result.AsEnumerable();
                }
                catch (ArgumentNullException ex)
                {
                    _logger.LogError(ex, "Route not found: {RouteId}", routeId);
                    throw new ArgumentException($"Route not found with ID: {routeId}", ex);
                }
            }) ?? [];
        }

        public async Task<IEnumerable<IStationLogic>> FindTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);

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
                    .GetTrafficLightsByRouteIdAsync(routeId, cancellationToken);

                var result = _stationLogics.Values.Join(
                    trafficLights,
                    s => s.StationId,
                    tl => tl.StationId,
                    (stationLogic, trafficLight) => stationLogic).ToList();

                _logger.LogDebug("Cached {Count} traffic light logics for route {RouteId}", result.Count, routeId);
                return result.AsEnumerable();
            }) ?? [];
        }

        public async Task<IEnumerable<IStationLogic>> FindNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId trafficLightId,
            CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);

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
                        .GetNextTrafficLightsAsync(routeId, trafficLightId, cancellationToken);

                    var result = _stationLogics.Values.Join(
                        nextTrafficLights,
                        s => s.StationId,
                        tl => tl.StationId,
                        (stationLogic, trafficLight) => stationLogic).ToList();

                    _logger.LogDebug("Cached {Count} next traffic light logics for route {RouteId}, traffic light {TrafficLightId}",
                        result.Count, routeId, trafficLightId);
                    return result.AsEnumerable();
                }
                catch (EntityNotFoundException ex)
                {
                    _logger.LogError(ex, "Route not found when getting next traffic lights: {RouteId}", routeId);
                    throw new InvalidOperationException($"Route not found. Cannot get next traffic lights for route: {routeId}", ex);
                }
            }) ?? [];
        }

        public void Dispose()
        {
            _initializationSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Refreshes all station logics and clears cache (internal method)
        /// </summary>
        internal async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refreshing station logics and clearing cache");

            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Clear cache first
                InvalidateCache();

                // Clear existing station logics
                _stationLogics.Clear();

                // Re-initialize
                _isInitialized = false;
                await InitializeAsync(cancellationToken);

                _logger.LogInformation("Station logics refreshed successfully");
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <summary>
        /// Invalidates all cached data (internal method)
        /// </summary>
        internal void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all cache entries");

            // Remove the main cache entry
            _cache.Remove(ALL_STATIONS_KEY);

            // Note: For route-specific caches, you might want to implement a more sophisticated
            // cache invalidation strategy, such as using cache tags or maintaining a list of cache keys
            // For now, we rely on cache expiration
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized)
                return;

            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_isInitialized)
                    return;

                await InitializeAsync(cancellationToken);
                _isInitialized = true;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        private async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing station logics from database");

            await using var scope = _serviceProvider.CreateAsyncScope();
            var stationLogicFactory = scope.ServiceProvider.GetRequiredService<IStationLogicFactory>();
            var repositoryManager = scope.ServiceProvider.GetRequiredService<IRepositoryManager>();

            var stations = await repositoryManager.StationRepository.GetAllAsync(cancellationToken);
            if (!stations.Any())
            {
                _logger.LogError("No stations found in database during initialization");
                throw new InvalidOperationException("There are no stations in the database.");
            }

            var stationLogics = stations.Select(station => stationLogicFactory.GetCreator(station).Create());

            foreach (var stationLogic in stationLogics)
            {
                _stationLogics.TryAdd(stationLogic.StationId, stationLogic);
            }

            _logger.LogInformation("Successfully initialized {Count} station logics", _stationLogics.Count);
        }
    }
}

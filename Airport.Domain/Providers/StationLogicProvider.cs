using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Domain.EventArgs.RouteEventArgs;
using Airport.Domain.EventArgs.StationEventArgs;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Airport.Domain.Providers
{
    public class StationLogicProvider : IStationLogicProvider
    {
        #region Fields
        private readonly IRepositoryManager _repoManager;
        private readonly IStationLogicFactory _stationLogicFactory;
        private readonly IMemoryCache _cache;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<StationLogicProvider> _logger;
        private readonly HashSet<ObjectId> _cacheStationsByRoutes;
        private readonly HashSet<ObjectId> _cacheTLsByRoutes;
        private readonly HashSet<ObjectId> _cacheNextTLs;
        private readonly ConcurrentDictionary<ObjectId, IStationLogic> _stations;
        private readonly ConcurrentDictionary<ObjectId, IStationChangedData> _stationsStateCache;
        private readonly AsyncSemaphore _operationSemaphore;
        private readonly AsyncSemaphore _updationStateCacheSem;
        private bool _isInitialized;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShortCacheExpiration = TimeSpan.FromMinutes(5);

        private const string ALL_STATIONS_KEY = "all_stations";
        private const string ROUTE_STATIONS_PREFIX = "route_stations_";
        private const string ROUTE_TRAFFIC_LIGHTS_PREFIX = "route_traffic_lights_";
        private const string NEXT_TRAFFIC_LIGHTS_PREFIX = "next_traffic_lights_";
        #endregion

        public StationLogicProvider(
            IRepositoryManager repoManager,
            IStationLogicFactory stationLogicFactory,
            IMemoryCache cache,
            IDomainEvents domainEvents,
            ILogger<StationLogicProvider> logger)
        {
            _repoManager = repoManager;
            _stationLogicFactory = stationLogicFactory;
            _cache = cache;
            _domainEvents = domainEvents;
            _logger = logger;
            _stations = new();
            _stationsStateCache = new();
            _cacheStationsByRoutes = new();
            _cacheTLsByRoutes = new();
            _cacheNextTLs = new();
            _updationStateCacheSem = new(1);
            _operationSemaphore = new(1);

            _domainEvents.DataRefreshed += OnDataRefreshedAsync;
            _domainEvents.SystemResetRequested += OnSystemResetRequestedAsync;

            _domainEvents.RouteUpdated += OnRouteUpdatedAsync;
            _domainEvents.RouteDeleted += OnRouteDeletedAsync;

            _domainEvents.StationCreated += OnStationCreatedAsync;
            _domainEvents.StationUpdated += OnStationUpdatedAsync;
            _domainEvents.StationDeleted += OnStationDeletedAsync;
        }

        public async Task<IEnumerable<IStationLogic>> GetByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var cacheKey = $"{ROUTE_STATIONS_PREFIX}{routeId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                IEnumerable<Station> routeStations;

                try
                {
                    routeStations = (await _repoManager.StationRepository
                        .GetStationsByRouteIdAsync(routeId, ct))
                        .ToList();
                }
                catch (EntityNotFoundException e)
                {
                    throw new LogicNotFoundException($"Route id: {routeId} not found.", e);
                }

                var allStations = await GetAllAsync(ct);

                var result = routeStations.Join(
                    allStations,
                    s => s.StationId,
                    sl => sl.StationId,
                    (station, stationLogic) => stationLogic)
                    .ToList();

                if (result.Count == 0)
                    throw new LogicNotFoundException($"No stations found for route id: {routeId}");

                entry.Size = result.Count;

                _logger.LogDebug("Cached {Count} station logics for route {RouteId}", result.Count, routeId);

                _cacheStationsByRoutes.Add(routeId);

                return result;

            }) ?? Enumerable.Empty<IStationLogic>();
        }

        public async Task<IEnumerable<IStationLogic>> GetTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var cacheKey = $"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{routeId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                var trafficLights = (await _repoManager.TrafficLightRepository
                    .GetTrafficLightsByRouteIdAsync(routeId, ct))
                    .ToList();

                var allStations = await GetAllAsync(ct);

                var result = allStations.Join(
                    trafficLights,
                    s => s.StationId,
                    tl => tl.StationId,
                    (stationLogic, trafficLight) => stationLogic)
                    .ToList();

                entry.Size = result.Count;

                _logger.LogDebug("Cached {Count} traffic light logics for route {RouteId}", result.Count, routeId);

                _cacheStationsByRoutes.Add(routeId);

                return result;

            }) ?? Enumerable.Empty<IStationLogic>();
        }

        public async Task<IEnumerable<IStationChangedData>> ProcessStationClearedAsync(
            IStationClearedEventArgs args,
            CancellationToken ct = default) => await UpdateStationsStateChangedAsync(args, ct);

        public async Task<IEnumerable<IStationChangedData>> ProcessFlightStartedAsync(
            IFlightRunStartedEventArgs args,
            CancellationToken ct = default)
        {
            var stationEventArgs = new StationClearedEventArgs
            {
                CurrentStationId = args.StationId,
                RouteId = args.RouteId,
                FlightId = args.Flight.FlightId,
                FlightType = args.Flight.ToFlightType(),
            };

            return await UpdateStationsStateChangedAsync(stationEventArgs, ct);
        }

        public void Dispose()
        {
            _domainEvents.DataRefreshed -= OnDataRefreshedAsync;
            _domainEvents.SystemResetRequested -= OnSystemResetRequestedAsync;

            _domainEvents.RouteUpdated -= OnRouteUpdatedAsync;
            _domainEvents.RouteDeleted -= OnRouteDeletedAsync;

            _domainEvents.StationCreated -= OnStationCreatedAsync;
            _domainEvents.StationUpdated -= OnStationUpdatedAsync;
            _domainEvents.StationDeleted -= OnStationDeletedAsync;

            _operationSemaphore?.Dispose();
            _updationStateCacheSem?.Dispose();
            _cache.Dispose();

            foreach (var entry in _stations)
                entry.Value.Dispose();

            _stations.Clear();

            GC.SuppressFinalize(this);
        }

        protected async Task<IEnumerable<IStationLogic>> GetAllAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(ALL_STATIONS_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                entry.Size = _stations.Count;

                _logger.LogDebug("Cached {Count} stations.", entry.Size);

                return _stations.Values;

            }) ?? Enumerable.Empty<IStationLogic>();
        }

        #region Event Handlers
        protected virtual async Task OnDataRefreshedAsync()
        {
            _logger.LogInformation("Refreshing station logics and clearing cache");

            using var _ = await _operationSemaphore.EnterAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Station logics refreshed successfully");

            await _domainEvents.RaiseStationLogicProviderRefreshedAsync();
        }

        protected virtual async Task OnSystemResetRequestedAsync()
        {
            _logger.LogInformation("Resetting station logics and clearing cache");

            using var _ = await _operationSemaphore.EnterAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Station logics reset successfully");

            await _domainEvents.RaiseStationLogicProviderResetAsync();
        }

        protected virtual async Task OnRouteUpdatedAsync(object? sender, IRouteUpdatedEventArgs args)
        {
            using var _ = await _operationSemaphore.EnterAsync();

            RemoveCacheEntriesWithRoutePrefix(args.RouteId);

            _logger.LogInformation("Station logics of route id: {RouteId} removed from cache.", args.RouteId);

            await _domainEvents.RaiseStationsByRouteUpdatedAsync(new StationsByRouteUpdatedEventArgs
            {
                RouteId = args.RouteId,
            });
        }
        // TODO: update instead of remove?
        protected virtual async Task OnRouteDeletedAsync(object? sender, IRouteDeletedEventArgs args)
        {
            using var _ = await _operationSemaphore.EnterAsync();

            RemoveCacheEntriesWithRoutePrefix(args.RouteId);

            _logger.LogInformation("Station logics of route id: {RouteId} removed from cache.", args.RouteId);
        }

        protected virtual async Task OnStationCreatedAsync(object? sender, IStationCreatedEventArgs args)
        {
            using var _ = await _operationSemaphore.EnterAsync();

            var station = await _repoManager.StationRepository.GetByIdAsync(args.StationId);

            var newStationLogic = _stationLogicFactory.GetCreator(station).Create();

            if (_stations.TryAdd(args.StationId, newStationLogic))
            {
                _stationsStateCache[args.StationId] = new StationChangedData
                {
                    StationId = args.StationId,
                };

                _cache.Remove(ALL_STATIONS_KEY);

                _logger.LogInformation("Station {StationId} added to cache.", args.StationId);
            }
        }

        protected virtual async Task OnStationUpdatedAsync(object? sender, IStationUpdatedEventArgs args)
        {
            using var _ = await _operationSemaphore.EnterAsync();
            // Get updated route directions
            var updatedStation = await _repoManager.StationRepository.GetByIdAsync(args.StationId);
            // Create new direction logic for each of them
            var updatedStationLogic = _stationLogicFactory.GetCreator(updatedStation).Create();
            // Remove from state cache
            _stationsStateCache.TryRemove(args.StationId, out var _);

            if (_stations.TryGetValue(args.StationId, out var oldStation))
            {
                _stations[args.StationId] = updatedStationLogic;

                _cache.Remove(ALL_STATIONS_KEY);

                oldStation.Dispose();

                _logger.LogInformation("Station id {StationId} updated on cache.", args.StationId);
            }

            // Remove from routes cache keys
            foreach (var routeId in await _repoManager.RouteRepository.IdsOfRoutesContainStationAsync(args.StationId))
                RemoveCacheEntriesWithRoutePrefix(routeId);

            await _domainEvents.RaiseStationLogicUpdatedAsync(new StationLogicUpdatedEventArgs
            {
                StationId = args.StationId,
            });

            await _domainEvents.RaiseStationProviderUpdatedAsync(new StationProviderUpdatedEventArgs
            {
                StationId = args.StationId,
            });
        }

        protected virtual async Task OnStationDeletedAsync(object? sender, IStationDeletedEventArgs args)
        {
            using var _ = await _operationSemaphore.EnterAsync();
            // Remove from state cache
            _stationsStateCache.TryRemove(args.StationId, out var stationData);
            // Remove logic
            if (_stations.TryRemove(args.StationId, out var stationLogic))
            {
                _cache.Remove(ALL_STATIONS_KEY);

                stationLogic.Dispose();

                _logger.LogInformation("Station id {StationId} removed from cache.", args.StationId);

                // No need to remove routeId cache keys
                // for deleting a route including its stations is not allowed.
            }
        }
        #endregion

        private void RemoveCacheEntriesWithRoutePrefix(ObjectId routeId)
        {
            _cache.Remove($"{ROUTE_STATIONS_PREFIX}{routeId}");
            _cache.Remove($"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{routeId}");
            _cache.Remove($"{NEXT_TRAFFIC_LIGHTS_PREFIX}{routeId}");
        }

        private void Clear()
        {
            InvalidateCache();

            foreach (var entry in _stations)
                entry.Value.Dispose();

            _stations.Clear();

            _stationsStateCache.Clear();

            _isInitialized = false;
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all station cache entries");

            _cache.Remove(ALL_STATIONS_KEY);

            foreach (var key in _cacheStationsByRoutes)
                _cache.Remove($"{ROUTE_STATIONS_PREFIX}{key}");

            foreach (var key in _cacheTLsByRoutes)
                _cache.Remove($"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{key}");

            foreach (var key in _cacheNextTLs)
                _cache.Remove($"{NEXT_TRAFFIC_LIGHTS_PREFIX}{key}");

            _cacheStationsByRoutes.Clear();
            _cacheTLsByRoutes.Clear();
            _cacheNextTLs.Clear();
        }

        private async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_isInitialized)
                return;

            using var _ = await _operationSemaphore.EnterAsync(ct);

            if (_isInitialized)
                return;

            await InitializeAsync(ct);

            _isInitialized = true;
        }

        private async Task InitializeAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Initializing station logics from repository");

            var stations = await _repoManager.StationRepository.GetAllAsync(ct);

            if (!stations.Any())
                throw new LogicProvisionFailedException("No routes exist.");

            Clear();

            foreach (var station in stations)
            {
                var stationLogic = _stationLogicFactory.GetCreator(station).Create();

                _stations.TryAdd(stationLogic.StationId, stationLogic);

                _stationsStateCache[stationLogic.StationId] = new StationChangedData
                {
                    StationId = stationLogic.StationId,
                };
            }

            _logger.LogInformation("Successfully initialized {StationsCount} station logics", _stations.Count);
        }

        // Prepare stations query for sending the state of stations
        private async Task<IEnumerable<IStationChangedData>> UpdateStationsStateChangedAsync(
            IStationClearedEventArgs args,
            CancellationToken ct = default)
        {
            IStationChangedData? nextStationData;
            IStationChangedData? oldStationData;
            List<IStationChangedData> returnValue = new();

            if (args.OldStationId is null)
            {
                nextStationData = new StationChangedData
                {
                    StationId = args.CurrentStationId!.Value,
                    Flight = new FlightInfo
                    {
                        FlightId = args.FlightId,
                        FlightType = args.FlightType,
                        RouteId = args.RouteId
                    }
                };
                _stationsStateCache[args.CurrentStationId!.Value] = nextStationData;
            }
            else if (args.CurrentStationId is null)
            {
                oldStationData = new StationChangedData
                {
                    StationId = args.OldStationId!.Value,
                };
                _stationsStateCache[args.OldStationId!.Value] = oldStationData;
            }
            else
            {
                oldStationData = new StationChangedData
                {
                    StationId = args.OldStationId!.Value,
                };
                nextStationData = new StationChangedData
                {
                    StationId = args.CurrentStationId!.Value,
                    Flight = new FlightInfo
                    {
                        FlightId = args.FlightId,
                        FlightType = args.FlightType,
                        RouteId = args.RouteId
                    }
                };

                using var _ = await _updationStateCacheSem.EnterAsync(ct);

                _stationsStateCache[args.OldStationId!.Value] = oldStationData;
                _stationsStateCache[args.CurrentStationId!.Value] = nextStationData;

                returnValue = _stationsStateCache.Values.ToList();
            }

            return returnValue;
        }

        #region Data Query Helpers
        private class StationChangedData : IStationChangedData
        {
            public ObjectId StationId { get; init; }
            public IFlightInfo? Flight { get; init; }
        }

        private class FlightInfo : IFlightInfo
        {
            public ObjectId FlightId { get; init; }
            public FlightType FlightType { get; init; }
            public ObjectId RouteId { get; init; }
        }
        #endregion
    }
}

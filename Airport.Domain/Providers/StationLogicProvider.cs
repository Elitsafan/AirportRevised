using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Domain.EventArgs.StationEventArgs;
using Airport.Domain.Helpers;
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
        private readonly ConcurrentDictionary<ObjectId, IStationChangedData> _stationsStateCache;
        private readonly AsyncSemaphore _initializationSemaphore;
        private bool _isInitialized;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShortCacheExpiration = TimeSpan.FromMinutes(5);

        //private const string ALL_STATIONS_KEY = "all_station_logics";
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
            _stationLogics = new();
            _stationsStateCache = new();
            _initializationSemaphore = new(1);

            _domainEvents.RouteCreated += OnRouteCreatedAsync;
            _domainEvents.RouteUpdated += OnRouteUpdatedAsync;
            _domainEvents.RouteDeleted += OnRouteDeletedAsync;

            _domainEvents.StationCreated += OnStationCreatedAsync;
            _domainEvents.StationDeleted += OnStationDeletedAsync;
            _domainEvents.StationUpdated += OnStationUpdatedAsync;

            _domainEvents.DataRefreshed += OnDataRefreshedAsync;
            _domainEvents.SystemResetRequested += OnSystemResetRequestedAsync;
        }

        //public async Task<IEnumerable<IStationLogic>> GetAllAsync(CancellationToken ct = default)
        //{
        //    await EnsureInitializedAsync(ct);

        //    return _cache.GetOrCreate(
        //        ALL_STATIONS_KEY,
        //        entry =>
        //        {
        //            entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
        //            entry.Size = 1;

        //            _logger.LogDebug($"Caching all station logics ({_stationLogics.Count} items)");
        //            return _stationLogics.Values;
        //        }) ?? Enumerable.Empty<IStationLogic>();
        //}

        public async Task<IStationLogic> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            if (_stationLogics.TryGetValue(id, out var stationLogic))
                return stationLogic;

            throw new LogicNotFoundException($"Station logic not found for Id: {id}");
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
                entry.Size = 1;

                await using var repositoryManager = _serviceProvider
                    .CreateAsyncScope()
                    .ServiceProvider
                    .GetRequiredService<IRepositoryManager>();

                IEnumerable<Station> stations;
                Route route;

                try
                {
                    route = await repositoryManager.RouteRepository.GetByIdAsync(routeId, ct);
                    stations = (await repositoryManager.StationRepository
                        .GetStationsByRouteAsync(route, ct))
                        .ToList();
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
                entry.Size = 1;

                await using var repositoryManager = _serviceProvider
                    .CreateAsyncScope()
                    .ServiceProvider
                    .GetRequiredService<IRepositoryManager>();

                var trafficLights = (await repositoryManager.TrafficLightRepository
                    .GetTrafficLightsByRouteIdAsync(routeId, ct))
                    .ToList();

                var result = _stationLogics.Values.Join(
                    trafficLights,
                    s => s.StationId,
                    tl => tl.StationId,
                    (stationLogic, trafficLight) => stationLogic)
                    .ToList();

                _logger.LogDebug($"Cached {result.Count} traffic light logics for route {routeId}");
                return result;
            }) ?? Enumerable.Empty<IStationLogic>();
        }

        public async Task<IEnumerable<IStationLogic>> GetNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId trafficLightId,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var cacheKey = $"{NEXT_TRAFFIC_LIGHTS_PREFIX}{routeId}_{trafficLightId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ShortCacheExpiration;
                entry.Size = 1;

                try
                {
                    await using var repositoryManager = _serviceProvider
                        .CreateAsyncScope()
                        .ServiceProvider
                        .GetRequiredService<IRepositoryManager>();

                    var nextTrafficLights = (await repositoryManager.TrafficLightRepository
                        .GetNextTrafficLightsAsync(routeId, trafficLightId, ct))
                        .ToList();

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
            }) ?? Enumerable.Empty<IStationLogic>();
        }

        public IEnumerable<IStationChangedData> ProcessStationCleared(
            IStationClearedEventArgs args,
            CancellationToken ct = default) => UpdateStationsStateChanged(args);

        public IEnumerable<IStationChangedData> ProcessFlightStarted(
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
            return UpdateStationsStateChanged(stationEventArgs);
        }

        public void Dispose()
        {
            _initializationSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Event Handlers
        protected virtual async Task OnDataRefreshedAsync() => await RefreshAsync();

        protected virtual async Task OnSystemResetRequestedAsync() => await RefreshAsync();

        protected virtual async Task OnRouteCreatedAsync(object? sender, IRouteCreatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            var newIds = ExtractStationIds(args.Directions);

            await using var scope = _serviceProvider.CreateAsyncScope();
            var routeRepo = scope.ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;

            var intersection = await routeRepo.GetIntersectedRoutesAsync(newIds);
            foreach (var route in intersection)
            {
                _cache.Remove($"{ROUTE_STATIONS_PREFIX}{route.RouteId}");
                _cache.Remove($"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{route.RouteId}");
                _cache.Remove($"{NEXT_TRAFFIC_LIGHTS_PREFIX}{route.RouteId}");
            }

            InvalidateCache();

            _logger.LogInformation($"Station logics of route Id: {args.RouteId} added to cache.");
        }

        protected virtual async Task OnRouteUpdatedAsync(object? sender, IRouteUpdatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            var newIds = ExtractStationIds(args.Directions);
            var oldIds = args.OldRoute.Directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .Except(newIds)
                .ToList();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var routeRepo = scope.ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;

            var oldIntersection = await routeRepo.GetIntersectedRoutesAsync(oldIds);
            foreach (var route in oldIntersection)
            {
                _cache.Remove($"{ROUTE_STATIONS_PREFIX}{route.RouteId}");
                _cache.Remove($"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{route.RouteId}");
                _cache.Remove($"{NEXT_TRAFFIC_LIGHTS_PREFIX}{route.RouteId}");
            }

            var newIntersection = await routeRepo.GetIntersectedRoutesAsync(newIds);
            foreach (var route in newIntersection)
            {
                _cache.Remove($"{ROUTE_STATIONS_PREFIX}{route.RouteId}");
                _cache.Remove($"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{route.RouteId}");
                _cache.Remove($"{NEXT_TRAFFIC_LIGHTS_PREFIX}{route.RouteId}");
            }

            InvalidateCache();

            _logger.LogInformation($"Station logics of route Id: {args.RouteId} updated on cache.");
        }

        protected virtual async Task OnRouteDeletedAsync(object? sender, IRouteDeletedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();
            var oldIds = ExtractStationIds(args.Directions);

            await using var scope = _serviceProvider.CreateAsyncScope();
            var routeRepo = scope.ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;

            var oldIntersection = await routeRepo.GetIntersectedRoutesAsync(oldIds);
            foreach (var route in oldIntersection)
            {
                _cache.Remove($"{ROUTE_STATIONS_PREFIX}{route.RouteId}");
                _cache.Remove($"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{route.RouteId}");
                _cache.Remove($"{NEXT_TRAFFIC_LIGHTS_PREFIX}{route.RouteId}");
            }

            InvalidateCache();

            _logger.LogInformation($"Station logics of route Id: {args.RouteId} removed from cache.");
        }

        protected virtual async Task OnStationCreatedAsync(object? sender, IStationCreatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var station = await scope
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .StationRepository
                .GetByIdAsync(args.StationId);
            var newStationLogic = _stationLogicFactory
                .GetCreator(station)
                .Create();
            if (_stationLogics.TryAdd(args.StationId, newStationLogic))
            {
                InvalidateCache();
                _logger.LogInformation($"Station {args.StationId} added to cache.");
            }
        }

        protected virtual async Task OnStationUpdatedAsync(object? sender, IStationUpdatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var repositoryManager = scope.ServiceProvider.GetRequiredService<IRepositoryManager>();
            var updatedStation = await repositoryManager.StationRepository
                .GetByIdAsync(args.StationId);
            var updatedStationLogic = _stationLogicFactory
                .GetCreator(updatedStation)
                .Create();
            _stationLogics[args.StationId] = updatedStationLogic;
            _cache.Remove($"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{args.StationId}");
            _cache.Remove($"{NEXT_TRAFFIC_LIGHTS_PREFIX}{args.StationId}");
            InvalidateCache();
        }

        protected virtual async Task OnStationDeletedAsync(object? sender, IStationDeletedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            _stationsStateCache.TryRemove(args.StationId, out var stationData);
            if (_stationLogics.TryRemove(args.StationId, out var stationLogic))
            {
                stationLogic.Dispose();
                _cache.Remove($"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{args.StationId}");
                _cache.Remove($"{NEXT_TRAFFIC_LIGHTS_PREFIX}{args.StationId}");
                InvalidateCache();
                _logger.LogInformation($"Station {args.StationId} removed from cache.");
            }
        }
        #endregion

        private List<ObjectId> ExtractStationIds(IEnumerable<Direction> directions) => directions
            .SelectMany(d => new[] { d.From, d.To })
            .Distinct()
            .ToList();

        private async Task RefreshAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Refreshing station logics and clearing cache");

            using var releaser = await _initializationSemaphore.EnterAsync(ct);

            InvalidateCache();

            _stationLogics.Clear();

            // Re-initialize
            _isInitialized = false;
            await InitializeAsync(ct);
            _isInitialized = true;

            _logger.LogInformation("Station logics refreshed successfully");
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all station cache entries");

            //_cache.Remove(ALL_STATIONS_KEY);

            foreach (var key in _stationLogics.Keys)
            {
                _cache.Remove($"{ROUTE_STATIONS_PREFIX}{key}");
                _cache.Remove($"{ROUTE_TRAFFIC_LIGHTS_PREFIX}{key}");
                _cache.Remove($"{NEXT_TRAFFIC_LIGHTS_PREFIX}{key}");
            }
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
            var stations = await scope.ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .StationRepository
                .GetAllAsync(ct);

            if (!stations.Any())
            {
                _logger.LogError("No stations found during initialization");
                throw new InvalidOperationException("There are no stations.");
            }

            foreach (var station in stations)
            {
                var stationLogic = _stationLogicFactory
                    .GetCreator(station)
                    .Create();
                _stationLogics.TryAdd(stationLogic.StationId, stationLogic);
                _stationsStateCache[stationLogic.StationId] = new StationChangedData
                {
                    StationId = stationLogic.StationId,
                };
            }
            _logger.LogInformation($"Successfully initialized {_stationLogics.Count} station logics");
        }

        // Prepare stations query for sending the state of stations
        private IEnumerable<IStationChangedData> UpdateStationsStateChanged(
            IStationClearedEventArgs args)
        {
            IStationChangedData? nextStationData;
            IStationChangedData? oldStationData;

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

                _stationsStateCache[args.OldStationId!.Value] = oldStationData;
                _stationsStateCache[args.CurrentStationId!.Value] = nextStationData;
            }

            return _stationsStateCache.Values;
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

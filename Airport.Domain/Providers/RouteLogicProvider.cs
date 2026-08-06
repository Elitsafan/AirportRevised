using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Airport.Domain.Providers
{
    public class RouteLogicProvider : IRouteLogicProvider
    {
        #region Fields
        private int _landingRoutesIdx;
        private int _departureRoutesIdx;
        private bool _isInitialized;
        private readonly ConcurrentDictionary<ObjectId, IRouteLogic> _landingRoutes;
        private readonly ConcurrentDictionary<ObjectId, IRouteLogic> _departureRoutes;
        private readonly AsyncSemaphore _operationSemaphore;
        private readonly AsyncSemaphore _landingIterationSemaphore;
        private readonly AsyncSemaphore _departureIterationSemaphore;
        private readonly IRepositoryManager _repoManager;
        private readonly IStationLogicProvider _stationProvider;
        private readonly ISectionLogicProvider _sectionProvider;
        private readonly IRouteLogicFactory _routeLogicFactory;
        private readonly IMemoryCache _cache;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<RouteLogicProvider> _logger;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);

        private const string LANDING_ROUTES_KEY = "landing_route_logics";
        private const string DEPARTURE_ROUTES_KEY = "departure_route_logics";
        #endregion

        public RouteLogicProvider(
            IRepositoryManager repoManager,
            IStationLogicProvider stationProvider,
            ISectionLogicProvider sectionProvider,
            IRouteLogicFactory routeLogicFactory,
            IMemoryCache cache,
            IDomainEvents domainEvents,
            ILogger<RouteLogicProvider> logger)
        {
            _repoManager = repoManager;
            _stationProvider = stationProvider;
            _sectionProvider = sectionProvider;
            _routeLogicFactory = routeLogicFactory;
            _cache = cache;
            _domainEvents = domainEvents;
            _logger = logger;
            _landingRoutes = new();
            _departureRoutes = new();
            _operationSemaphore = new(1);
            _landingIterationSemaphore = new(1);
            _departureIterationSemaphore = new(1);

            _domainEvents.SectionProviderReset += OnSectionProviderResetAsync;
            _domainEvents.SectionProviderRefreshed += OnSectionProviderRefreshedAsync;

            _domainEvents.RouteCreated += OnRouteCreatedAsync;
            _domainEvents.RouteUpdated += OnRouteUpdatedAsync;
            _domainEvents.RouteDeleted += OnRouteDeletedAsync;

            _domainEvents.StationProviderUpdated += OnStationProviderUpdatedAsync;
        }

        public async Task<IRouteLogic> GetNextRouteAsync(FlightType flightType, CancellationToken ct = default) =>
            // await EnsureInitializedAsync(ct);
            flightType == FlightType.Landing
                ? await GetNextLandingRouteAsync(ct)
                : await GetNextDepartureRouteAsync(ct);

        public void Dispose()
        {
            _domainEvents.SectionProviderReset -= OnSectionProviderResetAsync;
            _domainEvents.SectionProviderRefreshed -= OnSectionProviderRefreshedAsync;

            _domainEvents.RouteCreated -= OnRouteCreatedAsync;
            _domainEvents.RouteUpdated -= OnRouteUpdatedAsync;
            _domainEvents.RouteDeleted -= OnRouteDeletedAsync;

            _domainEvents.StationProviderUpdated -= OnStationProviderUpdatedAsync;

            _cache.Dispose();
            _operationSemaphore.Dispose();
            _landingIterationSemaphore.Dispose();
            _departureIterationSemaphore.Dispose();

            foreach (var route in _departureRoutes.Values)
                route.Dispose();
            foreach (var route in _landingRoutes.Values)
                route.Dispose();

            _departureRoutes.Clear();

            _landingRoutes.Clear();

            GC.SuppressFinalize(this);
        }

        #region Event Handlers
        protected virtual async Task OnSectionProviderResetAsync()
        {
            _logger.LogInformation("Resetting route logics and clearing cache.");

            using var _ = await _operationSemaphore.EnterAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Route logics reset successfully.");
        }

        protected virtual async Task OnSectionProviderRefreshedAsync()
        {
            _logger.LogInformation("Refreshing route logics and clearing cache.");

            using var _ = await _operationSemaphore.EnterAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Route logics refreshed successfully.");
        }

        protected virtual async Task OnRouteCreatedAsync(object? sender, IRouteCreatedEventArgs args)
        {
            await EnsureInitializedAsync();

            using var _ = await _operationSemaphore.EnterAsync();

            var route = await _repoManager.RouteRepository.GetByIdAsync(args.RouteId);

            var sections = (await _sectionProvider.GetByRouteIdAsync(args.RouteId)).ToList();

            List<IStationLogic>? standaloneTLs = null;

            if (args.StandaloneTLIds is not null)
                standaloneTLs = (await _stationProvider.GetTrafficLightsByRouteIdAsync(args.RouteId))
                    .IntersectBy(
                        args.StandaloneTLIds,
                        tl => tl.StationId)
                    .ToList();

            var routeLogic = await _routeLogicFactory.GetCreator(route, sections, standaloneTLs).CreateAsync();

            if (string.Compare(routeLogic.RouteName, FlightType.Departure.ToString(), false) == 0)
            {
                if (_departureRoutes.TryAdd(routeLogic.RouteId, routeLogic))
                {
                    _cache.Remove(DEPARTURE_ROUTES_KEY);

                    _departureRoutesIdx = -1;
                }
            }
            else if (string.Compare(routeLogic.RouteName, FlightType.Landing.ToString(), false) == 0)
            {
                if (_landingRoutes.TryAdd(routeLogic.RouteId, routeLogic))
                {
                    _cache.Remove(LANDING_ROUTES_KEY);

                    _landingRoutesIdx = -1;
                }
            }
            else throw new InvalidOperationException("Route name is invalid.");

            _logger.LogInformation("Route id: {RouteId} added to cache.", routeLogic.RouteId);
        }

        protected virtual async Task OnRouteUpdatedAsync(object? sender, IRouteUpdatedEventArgs args)
        {
            await EnsureInitializedAsync();

            using var _ = await _operationSemaphore.EnterAsync();

            var updatedRoute = await _repoManager.RouteRepository.GetByIdAsync(args.RouteId);

            var updatedSections = (await _sectionProvider
                .GetByRouteIdAsync(args.RouteId))
                .ToList();

            List<IStationLogic>? updatedStandaloneTLs = null;

            if (args.StandaloneTLIds is not null)
                updatedStandaloneTLs = (await _stationProvider.GetTrafficLightsByRouteIdAsync(args.RouteId))
                    .IntersectBy(
                        args.StandaloneTLIds,
                        tl => tl.StationId)
                    .ToList();

            var newRouteLogic = await _routeLogicFactory
                .GetCreator(updatedRoute, updatedSections, updatedStandaloneTLs)
                .CreateAsync();

            if (string.Compare(newRouteLogic.RouteName, FlightType.Departure.ToString(), false) == 0)
            {
                if (_departureRoutes.TryGetValue(newRouteLogic.RouteId, out var oldRouteLogic))
                {
                    _departureRoutes[newRouteLogic.RouteId] = newRouteLogic;

                    _cache.Remove(DEPARTURE_ROUTES_KEY);

                    _departureRoutesIdx = -1;

                    oldRouteLogic?.Dispose();
                }
            }
            else if (string.Compare(newRouteLogic.RouteName, FlightType.Landing.ToString(), false) == 0)
            {
                if (_landingRoutes.TryGetValue(newRouteLogic.RouteId, out var oldRouteLogic))
                {
                    _landingRoutes[newRouteLogic.RouteId] = newRouteLogic;

                    _cache.Remove(LANDING_ROUTES_KEY);

                    _landingRoutesIdx = -1;

                    oldRouteLogic?.Dispose();
                }
            }
            else throw new InvalidOperationException("Route name is invalid.");

            _logger.LogInformation("Route Id: {RouteId} updated on cache.", args.RouteId);
        }

        protected virtual async Task OnRouteDeletedAsync(object? sender, IRouteDeletedEventArgs args)
        {
            await EnsureInitializedAsync();

            using var _ = await _operationSemaphore.EnterAsync();

            if (_departureRoutes.TryRemove(args.RouteId, out var oldRouteLogic))
            {
                _cache.Remove(DEPARTURE_ROUTES_KEY);

                _departureRoutesIdx = -1;
            }
            else if (_landingRoutes.TryRemove(args.RouteId, out oldRouteLogic))
            {
                _cache.Remove(LANDING_ROUTES_KEY);

                _landingRoutesIdx = -1;
            }

            oldRouteLogic?.Dispose();

            _logger.LogInformation("Route Id: {RouteId} removed from cache.", args.RouteId);
        }

        protected virtual async Task OnStationProviderUpdatedAsync(object? sender, IStationProviderUpdatedEventArgs args)
        {
            await EnsureInitializedAsync();

            using var _ = await _operationSemaphore.EnterAsync();

            var relevantRouteIds = await _repoManager.RouteRepository.IdsOfRoutesContainStationAsync(args.StationId);

            foreach (var routeId in relevantRouteIds)
            {
                var updatedRoute = await _repoManager.RouteRepository.GetByIdAsync(routeId);

                var updatedSections = (await _sectionProvider
                    .GetByRouteIdAsync(routeId))
                    .ToList();

                var updatedStandaloneTLs = (await _stationProvider.GetTrafficLightsByRouteIdAsync(routeId))
                    .Join(
                        await _repoManager.TrafficLightRepository.GetStandaloneTLsAsync(routeId),
                        sl => sl.StationId,
                        tl => tl.StationId,
                        (sl, tl) => sl)
                    .ToList();

                var newRouteLogic = await _routeLogicFactory
                    .GetCreator(updatedRoute, updatedSections, updatedStandaloneTLs)
                    .CreateAsync();

                if (string.Compare(newRouteLogic.RouteName, FlightType.Departure.ToString(), false) == 0)
                {
                    if (_departureRoutes.TryGetValue(newRouteLogic.RouteId, out var oldRouteLogic))
                    {
                        _departureRoutes[newRouteLogic.RouteId] = newRouteLogic;

                        oldRouteLogic?.Dispose();
                    }
                }
                else if (string.Compare(newRouteLogic.RouteName, FlightType.Landing.ToString(), false) == 0)
                {
                    if (_landingRoutes.TryGetValue(newRouteLogic.RouteId, out var oldRouteLogic))
                    {
                        _landingRoutes[newRouteLogic.RouteId] = newRouteLogic;

                        oldRouteLogic?.Dispose();
                    }
                }
                else throw new InvalidOperationException("Route name is invalid.");

                _logger.LogInformation("Route Id: {RouteId} updated on cache.", routeId);
            }

            ResetRoutesCounters();

            _cache.Remove(LANDING_ROUTES_KEY);

            _cache.Remove(DEPARTURE_ROUTES_KEY);
        }
        #endregion

        private async Task<List<IRouteLogic>> GetLandingRoutesAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(LANDING_ROUTES_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                entry.Size = _landingRoutes.Count;

                return _landingRoutes.Values.ToList();
            })!;
        }

        private async Task<List<IRouteLogic>> GetDepartureRoutesAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(DEPARTURE_ROUTES_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                entry.Size = _departureRoutes.Count;

                return _departureRoutes.Values.ToList();
            })!;
        }

        private async Task<IRouteLogic> GetNextDepartureRouteAsync(CancellationToken ct = default)
        {
            var list = await GetDepartureRoutesAsync(ct);

            if (list.Count == 0)
                throw new LogicProvisionFailedException("Error while providing route.");

            using var _ = await _departureIterationSemaphore.EnterAsync(ct);

            _departureRoutesIdx = Interlocked.Increment(ref _departureRoutesIdx) % list.Count;

            return list[_departureRoutesIdx];
        }

        private async Task<IRouteLogic> GetNextLandingRouteAsync(CancellationToken ct = default)
        {
            var list = await GetLandingRoutesAsync(ct);

            if (list.Count == 0)
                throw new LogicProvisionFailedException("Error while providing route.");

            using var _ = await _landingIterationSemaphore.EnterAsync(ct);

            _landingRoutesIdx = Interlocked.Increment(ref _landingRoutesIdx) % list.Count;

            return list[_landingRoutesIdx];
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

            _logger.LogDebug("Routes cache was successfully populated.");
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all cache entries");

            _cache.Remove(LANDING_ROUTES_KEY);
            _cache.Remove(DEPARTURE_ROUTES_KEY);
        }

        private async Task InitializeAsync(CancellationToken ct = default)
        {
            var routes = await _repoManager.RouteRepository.GetAllAsync(ct);

            if (!routes.Any())
                throw new EntityNotFoundException("No routes found.");

            Clear();

            var allSections = await _sectionProvider.GetAllAsync(ct);

            foreach (Route route in routes)
            {
                var standaloneTLs = (await _stationProvider
                    .GetTrafficLightsByRouteIdAsync(route.RouteId, ct))
                    .ToList();

                allSections.TryGetValue(route.RouteId, out var sections);

                var routeLogic = await _routeLogicFactory
                    .GetCreator(route, sections, standaloneTLs)
                    .CreateAsync(ct);

                AddRouteLogic(routeLogic);
            }

            ResetRoutesCounters();
        }

        private void AddRouteLogic(IRouteLogic routeLogic)
        {
            if (string.Compare(routeLogic.RouteName, FlightType.Landing.ToString(), false) == 0)
                _landingRoutes.TryAdd(routeLogic.RouteId, routeLogic);
            else
                _departureRoutes.TryAdd(routeLogic.RouteId, routeLogic);
        }

        private void Clear()
        {
            InvalidateCache();

            foreach (var route in _landingRoutes.Values)
                route.Dispose();
            foreach (var route in _departureRoutes.Values)
                route.Dispose();

            _landingRoutes.Clear();

            _departureRoutes.Clear();
        }

        private void ResetRoutesCounters()
        {
            _departureRoutesIdx = -1;
            _landingRoutesIdx = -1;
        }
    }
}

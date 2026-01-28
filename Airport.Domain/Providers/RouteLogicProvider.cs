using Airport.Domain.Helpers;
using Airport.Domain.Repositories;
using Airport.Models.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Providers
{
    public class RouteLogicProvider : IRouteLogicProvider
    {
        #region Fields
        private int _countLandingRoutes;
        private int _countDepartureRoutes;
        private bool _isInitialized;
        private readonly List<IRouteLogic> _landingRoutes;
        private readonly List<IRouteLogic> _departureRoutes;
        private readonly AsyncSemaphore _initializationSemaphore;
        private readonly IServiceProvider _serviceProvider;
        private readonly IStationLogicProvider _stationLogicProvider;
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
            IServiceProvider serviceProvider,
            IStationLogicProvider stationLogicProvider,
            IRouteLogicFactory routeLogicFactory,
            IMemoryCache cache,
            IDomainEvents domainEvents,
            ILogger<RouteLogicProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _stationLogicProvider = stationLogicProvider;
            _routeLogicFactory = routeLogicFactory;
            _cache = cache;
            _domainEvents = domainEvents;
            _logger = logger;
            _landingRoutes = new();
            _departureRoutes = new();
            _initializationSemaphore = new(1);

            _domainEvents.StationCreated += OnStationCreatedAsync;
            _domainEvents.StationDeleted += OnStationDeletedAsync;
            _domainEvents.StationUpdated += OnStationUpdatedAsync;
            _domainEvents.DataRefreshed += OnDataRefreshedAsync;
            _domainEvents.SystemResetRequested += OnSystemResetRequestedAsync;
        }

        public async Task<IEnumerable<IRouteLogic>> GetLandingRoutesAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(LANDING_ROUTES_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                entry.Size = _landingRoutes!.Count;
                return _landingRoutes;
            })!;
        }

        public async Task<IEnumerable<IRouteLogic>> GetDepartureRoutesAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(DEPARTURE_ROUTES_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                entry.Size = _departureRoutes!.Count;
                return _departureRoutes;
            })!;
        }

        public async Task<IRouteLogic?> GetNextRouteAsync(FlightType flightType, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);
            return flightType == FlightType.Landing
                ? await GetNextLandingRouteAsync(ct)
                : await GetNextDepartureRouteAsync(ct);
        }

        public void Dispose()
        {
            _initializationSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        // Iterator for getting the next route
        private async Task<IRouteLogic?> GetNextDepartureRouteAsync(CancellationToken ct = default) =>
            (await GetDepartureRoutesAsync(ct)).Any()
            ? _departureRoutes[Interlocked.Increment(ref _countDepartureRoutes) % _departureRoutes.Count]
            : null;

        // Iterator for getting the next route
        private async Task<IRouteLogic?> GetNextLandingRouteAsync(CancellationToken ct = default) =>
            (await GetLandingRoutesAsync(ct)).Any()
            ? _landingRoutes[Interlocked.Increment(ref _countLandingRoutes) % _landingRoutes.Count]
            : null;

        private async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_isInitialized)
                return;

            using var releaser = await _initializationSemaphore.EnterAsync(ct);

            if (_isInitialized)
                return;

            await InitializeAsync(ct);

            _isInitialized = true;
            _logger.LogDebug("Routes cache was successfully populated.");
        }

        private async Task RefreshAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Refreshing route logics and clearing cache");

            using var releaser = await _initializationSemaphore.EnterAsync(ct);
            // Clear cache first
            InvalidateCache();

            // Clear existing route logics
            _landingRoutes.Clear();
            _departureRoutes.Clear();

            // Re-initialize
            _isInitialized = false;
            await InitializeAsync(ct);
            _isInitialized = true;

            _logger.LogDebug("Routes cache was successfully populated.");
            _logger.LogInformation("Route logics refreshed successfully");
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all cache entries");

            // Remove the main cache entry
            _cache.Remove(LANDING_ROUTES_KEY);
            _cache.Remove(DEPARTURE_ROUTES_KEY);
        }

        #region Event Handlers
        protected virtual async Task OnDataRefreshedAsync() => await RefreshAsync();

        protected virtual async Task OnSystemResetRequestedAsync() => await RefreshAsync();

        protected virtual async Task OnStationUpdatedAsync(object? sender, IStationUpdatedEventArgs args)
        {
            var releaser = await _initializationSemaphore.EnterAsync();
            try
            {
                InvalidateCache();
            }
            finally { releaser.Dispose(); }
        }

        protected virtual async Task OnStationDeletedAsync(object? sender, IStationDeletedEventArgs args)
        {
            var releaser = await _initializationSemaphore.EnterAsync();
            try
            {
                InvalidateCache();
            }
            finally { releaser.Dispose(); }
        }

        protected virtual async Task OnStationCreatedAsync(object? sender, IStationCreatedEventArgs args)
        {
            var releaser = await _initializationSemaphore.EnterAsync();
            try
            {
                InvalidateCache();
            }
            finally { releaser.Dispose(); }
        }
        #endregion

        private async Task InitializeAsync(CancellationToken ct)
        {
            ResetRoutesCounters();
            HashSet<IRouteSection> sections = new(new RouteSectionComparer());
            await using var scope = _serviceProvider.CreateAsyncScope();
            var routeRepository = scope
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;
            var routes = await GetRoutesAsync(routeRepository, ct);
            foreach (Route route in routes)
            {
                // gets the traffic lights of the route
                var trafficLights = new List<IStationLogic>(
                    await _stationLogicProvider.FindTrafficLightsByRouteIdAsync(route.RouteId, ct));
                foreach (IStationLogic trafficLight in trafficLights)
                {
                    var nextTrafficLights = (await _stationLogicProvider
                        .FindNextTrafficLightsAsync(route.RouteId, trafficLight.StationId, ct))
                        .ToArray();
                    if (nextTrafficLights.Length == 0)
                        continue;

                    // Gets the stations between the current trafficlight and the next traffic lights 
                    var stationsBetween = (await routeRepository.GetStationsBetweenAsync(
                        route,
                        trafficLight.StationId,
                        nextTrafficLights[0].StationId,
                        ct))
                        .ToArray();

                    // A common trafficlight between the section's destination
                    // and the next trafficlights, so that the destination of the section exists
                    // is the source of another section (traffic on the opposite direction).
                    var sectionExist = sections.SingleOrDefault(
                        rs => rs.RouteId == route.RouteId &&
                        rs.Destination.Intersect(nextTrafficLights).Any());

                    // add it to the section exists
                    if (sectionExist is not null)
                        sectionExist.AddToSource(trafficLight);
                    else await CreateNewSectionAsync(
                        sections,
                        route,
                        trafficLight,
                        nextTrafficLights,
                        stationsBetween,
                        ct);
                }
            }

            var sectionDetailsList = CreateSectionsDetails(sections);

            // Creates route logics
            IRouteLogic[] routeLogics = await CreateRoutesLogicAsync(
                routes,
                sectionDetailsList,
                ct);

            // Sets the route logics collections
            _landingRoutes.AddRange(routeLogics
                .Where(rl => string.Compare(rl.RouteName, FlightType.Landing.ToString(), false) == 0));
            _departureRoutes.AddRange(routeLogics
                .Where(rl => string.Compare(rl.RouteName, FlightType.Departure.ToString(), false) == 0));

            ResetRoutesCounters();
        }

        private void ResetRoutesCounters()
        {
            _countDepartureRoutes = -1;
            _countLandingRoutes = -1;
        }

        private async Task<IEnumerable<Route>> GetRoutesAsync(
            IRouteRepository routeRepository,
            CancellationToken ct = default)
        {
            var routes = await routeRepository.GetAllAsync(ct);
            if (!routes.Any())
                throw new EntityNotFoundException("No routes found.");
            return routes;
        }

        private static Dictionary<ISet<IStationLogic>, List<ObjectId>> GetHelperForCommonTrafficLights(
            HashSet<IRouteSection> sections)
        {
            Dictionary<ISet<IStationLogic>, List<ObjectId>> trafficLightsToRouteIds = new(
                new StationLogicSetComparer());
            foreach (IRouteSection section in sections)
                if (!trafficLightsToRouteIds.TryAdd(section.AllTrafficLights, new() { section.RouteId }))
                    trafficLightsToRouteIds[section.AllTrafficLights].Add(section.RouteId);
            return trafficLightsToRouteIds;
        }

        // Creates a new section with a source and a destination
        // according to the current traffic light and its next traffic lights and the stations between
        private async Task CreateNewSectionAsync(
            HashSet<IRouteSection> sections,
            Route route,
            IStationLogic trafficLight,
            IStationLogic[] nextTrafficLights,
            Station[] stationsBetween,
            CancellationToken ct = default)
        {
            var source = new IStationLogic[] { trafficLight };
            var allStations = source
                .Concat(nextTrafficLights)
                .Concat(await Task.WhenAll(
                    stationsBetween.Select(
                        async s => await _stationLogicProvider.GetStationLogicByIdAsync(s.StationId, ct))));
            sections.Add(new RouteSection(
                source,
                nextTrafficLights,
                allStations,
                route.RouteId));
        }

        private List<IRouteSectionDetails>? CreateSectionsDetails(HashSet<IRouteSection> sections)
        {
            if (sections.Count == 0)
                return null;
            var destSynchronizerDic = CreateDestSynchronizerDic(sections);
            var trafficLightsToRouteIds = GetHelperForCommonTrafficLights(sections);
            List<IRouteSectionDetails> sectionDetailsList = new();
            foreach (var kvp in trafficLightsToRouteIds)
            {
                // Calculates possible occupation:
                // sum of stations + occupation * each route,
                // so when all stations is occupied, there is still a place on the section for each route.
                int occupationCapacity = kvp.Key.Count + kvp.Value.Count;
                var commonSections = sections.IntersectBy(kvp.Value, section => section.RouteId);
                IEnumerable<ISet<IStationLogic>> commonKeys = commonSections
                    .Select(sec => sec.Destination)
                    .Intersect(destSynchronizerDic.Keys)
                    .Where(sec => destSynchronizerDic[sec] is not null);
                Dictionary<ISet<IStationLogic>, AsyncSemaphore> commonDestToSem = new();
                foreach (ISet<IStationLogic> key in commonKeys)
                    commonDestToSem.Add(key, destSynchronizerDic[key]!);
                ISectionSynchronizerDetails synchronizer = new SectionSynchronizerDetails(
                    commonSections,
                    commonDestToSem,
                    occupationCapacity);
                foreach (var routeId in kvp.Value)
                {
                    var section = commonSections.Single(
                        sec => sec.AllTrafficLights.SetEquals(kvp.Key) &&
                        sec.RouteId == routeId);
                    sectionDetailsList.Add(new RouteSectionDetails(section, synchronizer));
                }
            }
            return sectionDetailsList;
        }

        private Dictionary<ISet<IStationLogic>, AsyncSemaphore?> CreateDestSynchronizerDic(
            HashSet<IRouteSection> sections) => new(
                sections.GroupBy(
                    rs => rs.Destination,
                    (key, collection) => new KeyValuePair<ISet<IStationLogic>, AsyncSemaphore?>(
                        key,
                        collection.Count() > 1
                            ? new AsyncSemaphore(key.Count)
                            : null),
                    new StationLogicSetComparer()),
                new StationLogicSetComparer());

        private async Task<IRouteLogic[]> CreateRoutesLogicAsync(
            IEnumerable<Route> routes,
            List<IRouteSectionDetails>? sectionDetails,
            CancellationToken ct = default) => await Task.WhenAll(
                routes.Select(async r => await _routeLogicFactory
                    .GetCreator(r, sectionDetails?.FindAll(sd => sd.RouteSection.RouteId == r.RouteId))
                    .CreateAsync()));
    }
}

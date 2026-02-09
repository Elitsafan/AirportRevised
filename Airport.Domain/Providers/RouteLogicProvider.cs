using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Domain.Helpers;
using Airport.Domain.Repositories;
using Airport.Models.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Providers
{
    public class RouteLogicProvider : IRouteLogicProvider
    {
        #region Fields
        private int _landingRoutesIdx;
        private int _departureRoutesIdx;
        private bool _isInitialized;
        private IReadOnlyList<IRouteLogic> _landingRoutes;
        private IReadOnlyList<IRouteLogic> _departureRoutes;
        private readonly AsyncSemaphore _initializationSemaphore;
        private readonly AsyncSemaphore _landingIterationSemaphore;
        private readonly AsyncSemaphore _departureIterationSemaphore;
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
            _landingRoutes = new List<IRouteLogic>();
            _departureRoutes = new List<IRouteLogic>();
            _initializationSemaphore = new(1);
            _landingIterationSemaphore = new(1);
            _departureIterationSemaphore = new(1);

            _domainEvents.RouteCreated += OnRouteCreatedAsync;
            _domainEvents.RouteUpdated += OnRouteUpdatedAsync;
            _domainEvents.RouteDeleted += OnRouteDeletedAsync;

            _domainEvents.StationCreated += OnStationCreatedAsync;
            _domainEvents.StationDeleted += OnStationDeletedAsync;
            _domainEvents.StationUpdated += OnStationUpdatedAsync;

            _domainEvents.DataRefreshed += OnDataRefreshedAsync;
            _domainEvents.SystemResetRequested += OnSystemResetRequestedAsync;
        }

        public async Task<IReadOnlyList<IRouteLogic>> GetLandingRoutesAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(LANDING_ROUTES_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                entry.Size = _landingRoutes.Count;
                return _landingRoutes;
            })!;
        }

        public async Task<IReadOnlyList<IRouteLogic>> GetDepartureRoutesAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(DEPARTURE_ROUTES_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                entry.Size = _departureRoutes.Count;
                return _departureRoutes;
            })!;
        }

        public async Task<IRouteLogic> GetNextRouteAsync(FlightType flightType, CancellationToken ct = default) =>
            // await EnsureInitializedAsync(ct);
            flightType == FlightType.Landing
                ? await GetNextLandingRouteAsync(ct)
                : await GetNextDepartureRouteAsync(ct);

        public void Dispose()
        {
            _initializationSemaphore.Dispose();
            _landingIterationSemaphore.Dispose();
            _departureIterationSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Event Handlers
        protected virtual async Task OnDataRefreshedAsync() => await RefreshAsync();

        protected virtual async Task OnSystemResetRequestedAsync() => await RefreshAsync();

        protected virtual async Task OnRouteCreatedAsync(object? sender, IRouteCreatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            await CreateAndUpdateRoutesAsync(args.RouteId);

            InvalidateCache();

            _logger.LogInformation($"Route Id: {args.RouteId} added to cache.");
        }

        protected virtual async Task OnRouteUpdatedAsync(object? sender, IRouteUpdatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            await CreateAndUpdateRoutesAsync(args.RouteId);

            InvalidateCache();

            _logger.LogInformation($"Route Id: {args.RouteId} updated on cache.");
        }

        protected virtual async Task OnRouteDeletedAsync(object? sender, IRouteDeletedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            int deleted = 0;
            if (string.Compare(args.RouteName, FlightType.Departure.ToString(), false) == 0)
            {
                var list = _departureRoutes.ToList();
                list.RemoveAll(rl => rl.RouteId == args.RouteId);
            }
            else if (string.Compare(args.RouteName, FlightType.Landing.ToString(), false) == 0)
            {
                var list = _landingRoutes.ToList();
                deleted = list.RemoveAll(rl => rl.RouteId == args.RouteId);
            }
            if (deleted > 0)
            {
                InvalidateCache();
                _logger.LogInformation($"Route Id: {args.RouteId} removed from cache.");
            }
        }

        protected virtual async Task OnStationCreatedAsync(object? sender, IStationCreatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();
            InvalidateCache();
        }

        protected virtual async Task OnStationUpdatedAsync(object? sender, IStationUpdatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();
            InvalidateCache();
        }

        protected virtual async Task OnStationDeletedAsync(object? sender, IStationDeletedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();
            InvalidateCache();
        }
        #endregion

        private async Task<IRouteLogic> GetNextDepartureRouteAsync(CancellationToken ct = default)
        {
            var list = await GetDepartureRoutesAsync(ct);
            if (list.Count == 0)
                throw new LogicProvisionFailedException("Error while providing route.");

            using var _ = await _departureIterationSemaphore.EnterAsync(ct);
            _landingRoutesIdx = Interlocked.Increment(ref _landingRoutesIdx) % list.Count;
            return list[_landingRoutesIdx];
        }

        private async Task<IRouteLogic> GetNextLandingRouteAsync(CancellationToken ct = default)
        {
            var list = await GetLandingRoutesAsync(ct);
            if (list.Count == 0)
                throw new LogicProvisionFailedException("Error while providing route.");

            using var _ = await _landingIterationSemaphore.EnterAsync(ct);
            _departureRoutesIdx = Interlocked.Increment(ref _departureRoutesIdx) % list.Count;
            return list[_departureRoutesIdx];
        }

        private async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_isInitialized)
                return;

            using var _ = await _initializationSemaphore.EnterAsync(ct);

            if (_isInitialized)
                return;

            await InitializeAsync(ct);

            _isInitialized = true;
            _logger.LogDebug("Routes cache was successfully populated.");
        }

        private async Task RefreshAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Refreshing route logics and clearing cache");

            using var _ = await _initializationSemaphore.EnterAsync(ct);
            // Clear cache first
            InvalidateCache();

            // Clear existing route logics
            _landingRoutes = new List<IRouteLogic>();
            _departureRoutes = new List<IRouteLogic>();

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

            _cache.Remove(LANDING_ROUTES_KEY);
            _cache.Remove(DEPARTURE_ROUTES_KEY);
        }

        private async Task CreateAndUpdateRoutesAsync(ObjectId routeId, CancellationToken ct = default)
        {
            HashSet<IRouteSection> sections = new(new RouteSectionComparer());
            await using var scope = _serviceProvider.CreateAsyncScope();
            var routeRepository = scope
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;

            var intersectedRoute = await routeRepository.GetByIdAsync(routeId, ct);
            var routes = await routeRepository.GetIntersectedRoutesAsync(intersectedRoute, ct);
            foreach (Route route in routes)
            {
                // gets the traffic lights of the route
                var trafficLights = await _stationLogicProvider.GetTrafficLightsByRouteIdAsync(
                    route.RouteId, ct);
                foreach (IStationLogic trafficLight in trafficLights)
                {
                    var nextTrafficLights = (await _stationLogicProvider
                        .GetNextTrafficLightsAsync(route.RouteId, trafficLight.StationId, ct))
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

            var sectionDetailsList = await CreateSectionsDetailsAsync(sections);

            // Creates route logics
            var routeLogics = await CreateRoutesLogicAsync(
                routes,
                sectionDetailsList,
                ct);

            // Set the new/updated route logics collections
            var updatedLandingRoutes = new List<IRouteLogic>();
            var updatedDepartureRoutes = new List<IRouteLogic>();

            foreach (var routeLogic in routeLogics)
                if (string.Compare(routeLogic.RouteName, FlightType.Landing.ToString(), false) == 0)
                    updatedLandingRoutes.Add(routeLogic);
                else
                    updatedDepartureRoutes.Add(routeLogic);

            // Filter and assign landing routes
            _landingRoutes = _landingRoutes
                .ExceptBy(updatedLandingRoutes.Select(
                    rl => rl.RouteId),
                    rl => rl.RouteId)
                .Concat(updatedLandingRoutes)
                .ToList();

            // Filter and assign departure routes
            _departureRoutes = _departureRoutes
                .ExceptBy(updatedDepartureRoutes.Select(
                    rl => rl.RouteId),
                    rl => rl.RouteId)
                .Concat(updatedDepartureRoutes)
                .ToList();

            ResetRoutesCounters();
        }

        private async Task InitializeAsync(CancellationToken ct = default)
        {
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
                var trafficLights = await _stationLogicProvider.GetTrafficLightsByRouteIdAsync(
                    route.RouteId, ct);
                foreach (IStationLogic trafficLight in trafficLights)
                {
                    var nextTrafficLights = (await _stationLogicProvider
                        .GetNextTrafficLightsAsync(route.RouteId, trafficLight.StationId, ct))
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

            var sectionDetailsList = await CreateSectionsDetailsAsync(sections);

            // Creates route logics
            var routeLogics = await CreateRoutesLogicAsync(
                routes,
                sectionDetailsList,
                ct);

            // Sets the route logics collections
            var landingRoutes = new List<IRouteLogic>();
            var departureRoutes = new List<IRouteLogic>();

            foreach (var routeLogic in routeLogics)
                if (string.Compare(routeLogic.RouteName, FlightType.Landing.ToString(), false) == 0)
                    landingRoutes.Add(routeLogic);
                else
                    departureRoutes.Add(routeLogic);

            _landingRoutes = landingRoutes;
            _departureRoutes = departureRoutes;

            ResetRoutesCounters();
        }

        private void ResetRoutesCounters()
        {
            _departureRoutesIdx = -1;
            _landingRoutesIdx = -1;
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
                        async s => await _stationLogicProvider.GetByIdAsync(s.StationId, ct))));
            sections.Add(new RouteSection(
                source,
                nextTrafficLights,
                allStations,
                route.RouteId));
        }

        private async Task<List<IRouteSectionDetails>?> CreateSectionsDetailsAsync(
            HashSet<IRouteSection> sections)
        {
            if (sections.Count == 0)
                return null;
            var destSynchronizerDic = CreateDestSynchronizerDic(sections);
            var trafficLightsToRouteIds = GetHelperForCommonTrafficLights(sections);
            List<IRouteSectionDetails> sectionDetailsList = new();
            await using var scope = _serviceProvider.CreateAsyncScope();
            var sectionLogger = scope.ServiceProvider.GetRequiredService<ILogger<RouteSectionDetails>>();

            foreach (var kvp in trafficLightsToRouteIds)
            {
                // Calculate possible occupation:
                // sum of stations + occupation * each route,
                // so when all stations is occupied, there is still a place on the section for each route.
                int occupationCapacity = kvp.Key.Count + kvp.Value.Count;
                var commonSections = sections
                    .IntersectBy(kvp.Value, section => section.RouteId)
                    .ToHashSet(new RouteSectionComparer());
                var commonKeys = commonSections
                    .Select(sec => sec.Destination)
                    .Intersect(destSynchronizerDic.Keys, new StationLogicSetComparer())
                    .Where(sec => destSynchronizerDic[sec] is not null)
                    .ToHashSet(new StationLogicSetComparer());
                Dictionary<ISet<IStationLogic>, AsyncSemaphore> commonDestToSem = new(new StationLogicSetComparer());
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
                    sectionDetailsList.Add(new RouteSectionDetails(
                        section,
                        synchronizer,
                        _domainEvents,
                        sectionLogger));
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
            CancellationToken ct = default) => await Task.WhenAll(routes
                .ToList()
                .Select(async 
                    r => await _routeLogicFactory.GetCreator(r, sectionDetails?.FindAll(
                        sd => sd.RouteSection.RouteId == r.RouteId))
                .CreateAsync(ct)));
    }
}

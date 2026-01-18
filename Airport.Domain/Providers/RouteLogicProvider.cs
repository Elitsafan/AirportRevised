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
        private List<IRouteLogic> _landingRoutes;
        private List<IRouteLogic> _departureRoutes;
        private bool _isInitialized;
        private readonly AsyncSemaphore _initializationSemaphore;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<RouteLogicProvider> _logger;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);

        private const string ALL_ROUTES_KEY = "all_route_logics";
        #endregion

        public RouteLogicProvider(
            IServiceProvider serviceProvider,
            IMemoryCache cache,
            IDomainEvents domainEvents,
            ILogger<RouteLogicProvider> logger)
        {
            _serviceProvider = serviceProvider;
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

            return _landingRoutes;
        }

        public async Task<IEnumerable<IRouteLogic>> GetDepartureRoutesAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _departureRoutes;
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
        }

        /// <summary>
        /// Refreshes all route logics and clears cache
        /// </summary>
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

            _logger.LogInformation("Route logics refreshed successfully");
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all cache entries");

            // Remove the main cache entry
            _cache.Remove(ALL_ROUTES_KEY);
        }

        private async Task OnDataRefreshedAsync() => await RefreshAsync();

        private async Task OnSystemResetRequestedAsync() => await RefreshAsync();

        private async Task OnStationUpdatedAsync(object? sender, IStationUpdatedEventArgs args) =>
            await RefreshAsync();
        private async Task OnStationDeletedAsync(object? sender, IStationDeletedEventArgs args) =>
            await RefreshAsync();
        private async Task OnStationCreatedAsync(object? sender, IStationCreatedEventArgs args) =>
            await RefreshAsync();

        private async Task<RouteLogicProvider> InitializeAsync(CancellationToken ct = default)
        {
            _countDepartureRoutes = -1;
            _countLandingRoutes = -1;
            HashSet<IRouteSection> sections = new(new RouteSectionComparer());
            await using var scope = _serviceProvider.CreateAsyncScope();
            var routeLogicFactory = scope
                .ServiceProvider
                .GetRequiredService<IRouteLogicFactory>();
            var stationLogicProvider = scope
                .ServiceProvider
                .GetRequiredService<IStationLogicProvider>();
            var routeRepository = scope
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;
            var routes = await GetRoutesAsync(routeRepository, ct);
            foreach (Route route in routes)
            {
                try
                {
                    // gets the traffic lights of the route
                    var trafficLights = new List<IStationLogic>(
                        await stationLogicProvider.FindTrafficLightsByRouteIdAsync(route.RouteId, ct));
                    foreach (IStationLogic trafficLight in trafficLights)
                    {
                        var nextTrafficLights = (await stationLogicProvider
                            .FindNextTrafficLightsAsync(route.RouteId, trafficLight.StationId, ct))
                            .ToArray();
                        if (nextTrafficLights.Length == 0)
                            continue;
                        // Gets the stations between current traffic light and the next traffic lights 
                        var stationsBetween = (await routeRepository.GetStationsBetweenAsync(
                            route,
                            trafficLight.StationId,
                            nextTrafficLights[0].StationId,
                            ct))
                            .ToArray();
                        ValidateRouteStructure(route, stationsBetween);
                        var section = sections.SingleOrDefault(
                            rs => rs.RouteId == route.RouteId &&
                            rs.Destination.Intersect(nextTrafficLights).Any());
                        // If section has a common traffic light with the next traffic lights
                        // adds it to the section that already exists
                        if (section is not null)
                            section.AddToSource(trafficLight);
                        else
                            await CreateNewSectionAsync(
                                sections,
                                stationLogicProvider,
                                route,
                                trafficLight,
                                nextTrafficLights,
                                stationsBetween,
                                ct);
                    }
                }
                catch (LogicNotFoundException e) { _logger.LogError(e, "Unable to process route."); }
                catch (InvalidRouteStructureException e) { _logger.LogError(e, null); }
            }

            var sectionDetailsList = CreateSectionsDetails(sections);

            // Creates route logics
            IRouteLogic[] routeLogics = await CreateRoutesLogicAsync(
                routeLogicFactory,
                routes,
                sectionDetailsList,
                ct);

            // Sets the route logics collections
            _landingRoutes = new List<IRouteLogic>(routeLogics
                .Where(rl => string.Compare(rl.RouteName, FlightType.Landing.ToString(), false) == 0));
            _departureRoutes = new List<IRouteLogic>(routeLogics
                .Where(rl => string.Compare(rl.RouteName, FlightType.Departure.ToString(), false) == 0));
            if (_landingRoutes.Count == 0 && _departureRoutes.Count == 0)
                throw new InvalidOperationException("Must have at least one route logic.");
            return this;
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
        private static async Task CreateNewSectionAsync(
            HashSet<IRouteSection> sections,
            IStationLogicProvider stationLogicProvider,
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
                        async s => await stationLogicProvider.GetStationLogicByIdAsync(s.StationId, ct))));
            sections.Add(new RouteSection(
                source,
                nextTrafficLights,
                allStations,
                route.RouteId));
        }

        private static void ValidateRouteStructure(Route route, Station[] stationsBetween)
        {
            if (stationsBetween.Length == 0)
                throw new InvalidRouteStructureException(
                    $"{route.RouteName} Route ID: {route.RouteId}:" +
                    "Must have least one station between two traffic lights. " +
                    "Route will not be provided." +
                    "\nProceed on processing routes.");
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
            IRouteLogicFactory routeLogicFactory,
            IEnumerable<Route> routes,
            List<IRouteSectionDetails>? sectionDetails,
            CancellationToken ct = default) => await Task.WhenAll(
                routes.Select(async r => await routeLogicFactory
                    .GetCreator(r, sectionDetails?.FindAll(sd => sd.RouteSection.RouteId == r.RouteId))
                    .CreateAsync()));
    }
}

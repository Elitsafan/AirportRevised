using Airport.Domain.Helpers;
using Airport.Models.Enums;

namespace Airport.Domain.Providers
{
    public class RouteLogicProvider : IRouteLogicProvider
    {
        #region Fields
        private int _countLandingRoutes;
        private int _countDepartureRoutes;
        private List<IRouteLogic> _landingRoutes = null!;
        private List<IRouteLogic> _departureRoutes = null!;
        private ILogger<RouteLogicProvider>? _logger;
        #endregion

        public static async Task<RouteLogicProvider> CreateAsync(IServiceProvider serviceProvider)
        {
            try
            {
                return await new RouteLogicProvider().InitializeAsync(serviceProvider);
            }
            catch (EntityNotFoundException e)
            {
                throw new InvalidOperationException(e.Message);
            }
        }

        public IEnumerable<IRouteLogic> LandingRoutes => _landingRoutes;
        public IEnumerable<IRouteLogic> DepartureRoutes => _departureRoutes;

        public IRouteLogic? GetNextRoute(FlightType flightType) => flightType == FlightType.Landing
            ? GetNextLandingRoute()
            : GetNextDepartureRoute();

        // Iterator for getting the next route
        private IRouteLogic? GetNextDepartureRoute() => DepartureRoutes.Any()
            ? _departureRoutes[Interlocked.Increment(ref _countDepartureRoutes) % _departureRoutes.Count]
            : null;

        // Iterator for getting the next route
        private IRouteLogic? GetNextLandingRoute() => LandingRoutes.Any()
            ? _landingRoutes[Interlocked.Increment(ref _countLandingRoutes) % _landingRoutes.Count]
            : null;

        private RouteLogicProvider()
        {
        }

        private async Task<RouteLogicProvider> InitializeAsync(IServiceProvider serviceProvider)
        {
            _countDepartureRoutes = -1;
            _countLandingRoutes = -1;
            HashSet<IRouteSection> sections = new(new RouteSectionComparer());
            _logger = serviceProvider.GetRequiredService<ILogger<RouteLogicProvider>>();
            var routeLogicFactory = serviceProvider.GetRequiredService<IRouteLogicFactory>();
            var stationLogicProvider = serviceProvider.GetRequiredService<IStationLogicProvider>();
            var routeRepository = serviceProvider
                .CreateAsyncScope()
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;
            var routes = await routeRepository.GetAllAsync();
            if (!routes.Any())
                throw new EntityNotFoundException("No routes found.");
            foreach (Route route in routes)
            {
                try
                {
                    // gets the traffic lights of the route
                    var trafficLights = new List<IStationLogic>(
                        await stationLogicProvider.FindTrafficLightsByRouteIdAsync(route.RouteId));
                    foreach (IStationLogic trafficLight in trafficLights)
                    {
                        var nextTrafficLights = (await stationLogicProvider
                            .FindNextTrafficLightsAsync(route.RouteId, trafficLight.StationId))
                            .ToArray();
                        if (nextTrafficLights.Length == 0)
                            continue;
                        // Gets the stations between current traffic light and the next traffic lights 
                        var stationsBetween = (await routeRepository.GetStationsBetweenAsync(
                            route,
                            trafficLight.StationId,
                            nextTrafficLights[0].StationId))
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
                                stationsBetween);
                    }
                }
                catch (LogicNotFoundException e) { _logger.LogError(e, "Unable to process route."); }
                catch (InvalidRouteStructureException e) { _logger.LogError(e, null); }
            }

            List<IRouteSectionDetails>? sectionDetailsList = null;
            if (sections.Count > 0)
            {
                Dictionary<ISet<IStationLogic>, AsyncSemaphore?> destinationSynchronizerDic = new(
                    sections.GroupBy(
                        rs => rs.Destination,
                        (key, collection) => new KeyValuePair<ISet<IStationLogic>, AsyncSemaphore?>(
                            key,
                            collection.Count() > 1
                                ? new AsyncSemaphore(key.Count)
                                : null),
                        new StationLogicSetComparer()),
                    new StationLogicSetComparer());

                Dictionary<ISet<IStationLogic>, List<ObjectId>> trafficLightsToRouteIds =
                    CreateHelperForCommonTrafficLights(sections);

                CreateSectionsDetails(
                    sections,
                    ref sectionDetailsList,
                    destinationSynchronizerDic,
                    trafficLightsToRouteIds);
            }

            // Creates route logics
            IRouteLogic[] routeLogics = await CreateRoutesLogicAsync(routeLogicFactory, routes, sectionDetailsList);

            // Sets the route logics collections
            _landingRoutes = new List<IRouteLogic>(routeLogics
                .Where(rl => string.Compare(rl.RouteName, FlightType.Landing.ToString(), false) == 0));
            _departureRoutes = new List<IRouteLogic>(routeLogics
                .Where(rl => string.Compare(rl.RouteName, FlightType.Departure.ToString(), false) == 0));
            if (_landingRoutes.Count == 0 && _departureRoutes.Count == 0)
                throw new InvalidOperationException("Must have at least one route logic.");
            return this;
        }

        private static Dictionary<ISet<IStationLogic>, List<ObjectId>> CreateHelperForCommonTrafficLights(
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
            Station[] stationsBetween)
        {
            var source = new IStationLogic[] { trafficLight };
            var allStations = source
                .Concat(nextTrafficLights)
                .Concat(await Task.WhenAll(
                    stationsBetween.Select(
                        async s => await stationLogicProvider.GetStationLogicByIdAsync(s.StationId))));
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

        private void CreateSectionsDetails(
            HashSet<IRouteSection> sections,
            ref List<IRouteSectionDetails>? sectionDetailsList,
            Dictionary<ISet<IStationLogic>, AsyncSemaphore?> destinationSynchronizerDic,
            Dictionary<ISet<IStationLogic>, List<ObjectId>> trafficLightsToRouteIds)
        {
            sectionDetailsList = new();
            foreach (var kvp in trafficLightsToRouteIds)
            {
                // Calculates possible occupation:
                // sum of stations + occupation * each route,
                // so when all stations is occupied, there is still a place on the section for each route.
                int occupationCapacity = kvp.Key.Count + kvp.Value.Count;
                var commonSections = sections.IntersectBy(kvp.Value, section => section.RouteId);
                IEnumerable<ISet<IStationLogic>> commonKeys = commonSections
                    .Select(sec => sec.Destination)
                    .Intersect(destinationSynchronizerDic.Keys)
                    .Where(sec => destinationSynchronizerDic[sec] is not null);
                Dictionary<ISet<IStationLogic>, AsyncSemaphore> commonDestToSem = new();
                foreach (ISet<IStationLogic> key in commonKeys)
                    commonDestToSem.Add(key, destinationSynchronizerDic[key]!);
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
        }

        private async Task<IRouteLogic[]> CreateRoutesLogicAsync(
            IRouteLogicFactory routeLogicFactory,
            IEnumerable<Route> routes,
            List<IRouteSectionDetails>? sectionDetails) => await Task.WhenAll(
                routes.Select(async r => await routeLogicFactory
                    .GetCreator(r, sectionDetails?.FindAll(sd => sd.RouteSection.RouteId == r.RouteId))
                    .CreateAsync()));
    }
}

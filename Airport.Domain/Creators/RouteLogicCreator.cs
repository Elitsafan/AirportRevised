namespace Airport.Domain.Creators
{
    internal class RouteLogicCreator : IRouteLogicCreator
    {
        #region Fields
        private readonly Route _route;
        private readonly ILogger<RouteLogic> _logger;
        private readonly IDirectionLogicProvider _directionProvider;
        private readonly IStationLogicProvider _stationProvider;
        private readonly List<ISectionLogic> _sections;
        private readonly List<IStationLogic> _standaloneTrafficLights;
        #endregion

        public RouteLogicCreator(
            Route route,
            IEnumerable<ISectionLogic>? sections,
            IEnumerable<IStationLogic>? standaloneTrafficLights,
            IDirectionLogicProvider directionProvider,
            IStationLogicProvider stationProvider,
            ILogger<RouteLogic> logger)
        {
            _route = route;
            _sections = sections is null
                ? new()
                : sections.ToList();
            _standaloneTrafficLights = standaloneTrafficLights is null
                ? new()
                : standaloneTrafficLights.ToList();
            _directionProvider = directionProvider;
            _stationProvider = stationProvider;
            _logger = logger;
        }

        public async Task<IRouteLogic> CreateAsync(CancellationToken ct = default)
        {
            var stations = (await _stationProvider.GetByRouteIdAsync(_route.RouteId, ct)).ToList();
            var directions = (await _directionProvider.GetByRouteIdAsync(_route.RouteId, ct)).ToList();
            var sectionTrafficLights = _sections
                .SelectMany(s => s.TrafficLights)
                .Distinct()
                .ToList();

            return new RouteLogic(
                _route,
                _logger,
                _sections,
                stations,
                directions,
                _standaloneTrafficLights,
                sectionTrafficLights);
        }
    }
}

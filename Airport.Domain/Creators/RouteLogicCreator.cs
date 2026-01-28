namespace Airport.Domain.Creators
{
    internal class RouteLogicCreator : IRouteLogicCreator
    {
        #region Fields
        private readonly Route _route;
        private readonly ILogger<RouteLogic> _logger;
        private readonly IDirectionLogicProvider _directionLogicProvider;
        private readonly IStationLogicProvider _stationLogicProvider;
        private readonly IEnumerable<IRouteSectionDetails>? _sections;
        #endregion

        public RouteLogicCreator(
            Route route,
            ILogger<RouteLogic> logger,
            IEnumerable<IRouteSectionDetails>? sections,
            IDirectionLogicProvider directionLogicProvider,
            IStationLogicProvider stationLogicProvider)
        {
            _route = route;
            _logger = logger;
            _sections = sections;
            _directionLogicProvider = directionLogicProvider;
            _stationLogicProvider = stationLogicProvider;
        }

        public async Task<IRouteLogic> CreateAsync()
        {
            List<IStationLogic> stations;
            List<IDirectionLogic> directions;
            var trafficLights = new List<IStationLogic>(_sections?
                .SelectMany(s => s.RouteSection.AllTrafficLights)
                .Distinct() ?? Enumerable.Empty<IStationLogic>());

            stations = new List<IStationLogic>(await _stationLogicProvider.FindStationLogicsByRouteIdAsync(_route.RouteId));
            directions = new List<IDirectionLogic>(await _directionLogicProvider.GetDirectionsByRouteIdAsync(_route.RouteId));

            return new RouteLogic(
                _route,
                _logger,
                _sections,
                stations,
                directions,
                trafficLights);
        }
    }
}

namespace Airport.Domain.Creators
{
    internal class RouteLogicCreator : IRouteLogicCreator
    {
        #region Fields
        private readonly Route _route;
        private readonly ILogger<RouteLogic> _logger;
        private readonly IDirectionLogicProvider _directionLogicProvider;
        private readonly IStationLogicProvider _stationLogicProvider;
        private readonly List<IRouteSectionDetails> _sections;
        #endregion

        public RouteLogicCreator(
            Route route,
            IEnumerable<IRouteSectionDetails>? sections,
            IDirectionLogicProvider directionLogicProvider,
            IStationLogicProvider stationLogicProvider,
            ILogger<RouteLogic> logger)
        {
            _route = route;
            _sections = sections is null
                ? new()
                : sections.ToList();
            _directionLogicProvider = directionLogicProvider;
            _stationLogicProvider = stationLogicProvider;
            _logger = logger;
        }

        public async Task<IRouteLogic> CreateAsync(CancellationToken ct = default)
        {
            var stations = (await _stationLogicProvider.GetByRouteIdAsync(_route.RouteId)).ToList();
            var directions = (await _directionLogicProvider.GetByRouteIdAsync(_route.RouteId)).ToList();
            var trafficLights = _sections.SelectMany(
                s => s.RouteSection.AllTrafficLights)
                .Distinct()
                .ToList();

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

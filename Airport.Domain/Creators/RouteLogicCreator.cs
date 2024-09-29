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
            IEnumerable<IRouteSectionDetails>? sections,
            ILogger<RouteLogic> logger,
            IDirectionLogicProvider directionLogicProvider,
            IStationLogicProvider stationLogicProvider)
        {
            _route = route;
            _sections = sections;
            _logger = logger;
            _directionLogicProvider = directionLogicProvider;
            _stationLogicProvider = stationLogicProvider;
        }

        public async Task<IRouteLogic> CreateAsync() => await RouteLogic.CreateAsync(
            _route,
            _sections,
            _logger,
            _directionLogicProvider,
            _stationLogicProvider);
    }
}

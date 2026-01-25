namespace Airport.Domain.Factories
{
    public class RouteLogicFactory : IRouteLogicFactory
    {
        #region Fields
        private readonly ILogger<RouteLogic> _logger;
        private readonly IDirectionLogicProvider _directionLogicProvider;
        private readonly IStationLogicProvider _stationLogicProvider;
        #endregion

        public RouteLogicFactory(
            ILogger<RouteLogic> logger,
            IDirectionLogicProvider directionLogicProvider,
            IStationLogicProvider stationLogicProvider)
        {
            _logger = logger;
            _directionLogicProvider = directionLogicProvider;
            _stationLogicProvider = stationLogicProvider;
        }

        public IRouteLogicCreator GetCreator(Route route, IEnumerable<IRouteSectionDetails>? sections)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            return new RouteLogicCreator(
                route,
                _logger,
                sections,
                _directionLogicProvider,
                _stationLogicProvider);
        }
    }
}

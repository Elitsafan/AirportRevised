namespace Airport.Domain.Factories
{
    public class RouteLogicFactory : IRouteLogicFactory
    {
        #region Fields
        private readonly IDirectionLogicProvider _directionProvider;
        private readonly IStationLogicProvider _stationProvider;
        private readonly ILogger<RouteLogic> _logger;
        #endregion

        public RouteLogicFactory(IDirectionLogicProvider directionProvider, IStationLogicProvider stationProvider, ILogger<RouteLogic> logger)
        {
            _directionProvider = directionProvider;
            _stationProvider = stationProvider;
            _logger = logger;
        }

        public IRouteLogicCreator GetCreator(Route route, IEnumerable<ISectionLogic>? sections, IEnumerable<IStationLogic>? standaloneTLs)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            return new RouteLogicCreator(
                route,
                sections?.ToList(),
                standaloneTLs?.ToList(),
                _directionProvider,
                _stationProvider,
                _logger);
        }
    }
}

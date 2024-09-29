namespace Airport.Domain.Factories
{
    public class RouteLogicFactory : IRouteLogicFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RouteLogicFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IRouteLogicCreator GetCreator(Route route, IEnumerable<IRouteSectionDetails>? sections)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));
            var logger = _serviceProvider.GetRequiredService<ILogger<RouteLogic>>();
            var directionLogicProvider = _serviceProvider.GetRequiredService<IDirectionLogicProvider>();
            var stationLogicProvider = _serviceProvider.GetRequiredService<IStationLogicProvider>();

            return new RouteLogicCreator(
                route,
                sections,
                logger,
                directionLogicProvider,
                stationLogicProvider);
        }
    }
}

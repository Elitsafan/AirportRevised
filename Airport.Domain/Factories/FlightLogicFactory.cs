namespace Airport.Domain.Factories
{
    public class FlightLogicFactory : IFlightLogicFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public FlightLogicFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IFlightLogicCreator GetCreator(Flight flight)
        {
            if (flight is null)
                throw new ArgumentNullException(nameof(flight));
            var logger = _serviceProvider.GetRequiredService<ILogger<FlightLogic>>();
            var routeLogicProvider = _serviceProvider.GetRequiredService<IRouteLogicProvider>();

            return flight switch
            {
                Departure => new DepartureLogicCreator(
                    (Departure)flight,
                    routeLogicProvider.GetNextRoute(Models.Enums.FlightType.Departure)!,
                    logger),
                Landing => new LandingLogicCreator(
                    (Landing)flight,
                    routeLogicProvider.GetNextRoute(Models.Enums.FlightType.Landing)!,
                    logger),
                _ => throw new ArgumentException("Unknown type of flight.")
            };
        }
    }
}

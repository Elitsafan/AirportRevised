namespace Airport.Domain.Creators
{
    internal class DepartureLogicCreator : IFlightLogicCreator
    {
        #region Fields
        private readonly Departure _departure;
        private readonly IRouteLogic _routeLogic;
        private readonly ILogger<FlightLogic> _logger;
        private readonly IDomainEvents _domainEvents;
        #endregion

        public DepartureLogicCreator(
            Departure departure,
            IRouteLogic routeLogic,
            IDomainEvents domainEvents,
            ILogger<FlightLogic> logger)
        {
            _departure = departure;
            _routeLogic = routeLogic;
            _domainEvents = domainEvents;
            _logger = logger;
        }

        public IFlightLogic Create() => new FlightLogic(_departure, _routeLogic, _domainEvents, _logger);
    }
}

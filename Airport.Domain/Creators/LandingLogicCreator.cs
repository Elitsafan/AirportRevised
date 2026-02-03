namespace Airport.Domain.Creators
{
    internal class LandingLogicCreator : IFlightLogicCreator
    {
        #region Fields
        private readonly Landing _landing;
        private readonly ILogger<FlightLogic> _logger;
        private readonly IRouteLogic _routeLogic;
        private readonly IDomainEvents _domainEvents;
        #endregion

        public LandingLogicCreator(
            Landing landing,
            IRouteLogic routeLogic,
            IDomainEvents domainEvents,
            ILogger<FlightLogic> logger)
        {
            _landing = landing;
            _routeLogic = routeLogic;
            _domainEvents = domainEvents;
            _logger = logger;
        }

        public IFlightLogic Create() => new FlightLogic(_landing, _routeLogic, _domainEvents, _logger);
    }
}

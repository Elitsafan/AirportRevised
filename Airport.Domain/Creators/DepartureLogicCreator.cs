namespace Airport.Domain.Creators
{
    internal class DepartureLogicCreator : IFlightLogicCreator
    {
        #region Fields
        private readonly Departure _departure;
        private readonly IRouteLogic _routeLogic;
        private readonly ILogger<FlightLogic> _logger;
        #endregion

        public DepartureLogicCreator(Departure departure, IRouteLogic routeLogic, ILogger<FlightLogic> logger)
        {
            _departure = departure;
            _logger = logger;
            _routeLogic = routeLogic;
        }

        public async Task<IFlightLogic> CreateAsync() =>
            await Task.FromResult<IFlightLogic>(new FlightLogic(_departure, _routeLogic, _logger));
    }
}

namespace Airport.Domain.Creators
{
    internal class LandingLogicCreator : IFlightLogicCreator
    {
        #region Fields
        private readonly Landing _landing;
        private readonly ILogger<FlightLogic> _logger;
        private readonly IRouteLogic _routeLogic;
        #endregion

        public LandingLogicCreator(Landing landing, IRouteLogic routeLogic, ILogger<FlightLogic> logger)
        {
            _landing = landing;
            _logger = logger;
            _routeLogic = routeLogic;
        }

        public async Task<IFlightLogic> CreateAsync() =>
            await Task.FromResult<IFlightLogic>(new FlightLogic(_landing, _routeLogic, _logger));
    }
}

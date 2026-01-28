using Airport.Models.Enums;

namespace Airport.Domain.Factories
{
    public class FlightLogicFactory : IFlightLogicFactory
    {
        #region Fields
        private readonly IRouteLogicProvider _routeLogicProvider;
        private readonly ILogger<FlightLogic> _logger;
        #endregion

        public FlightLogicFactory(IRouteLogicProvider routeLogicProvider, ILogger<FlightLogic> logger)
        {
            _routeLogicProvider = routeLogicProvider;
            _logger = logger;
        }

        public async Task<IFlightLogicCreator> GetCreatorAsync(Flight flight, CancellationToken ct = default)
        {
            if (flight is null)
                throw new ArgumentNullException(nameof(flight));

            return flight switch
            {
                Departure => new DepartureLogicCreator(
                    (Departure)flight,
                    (await _routeLogicProvider.GetNextRouteAsync(FlightType.Departure, ct))!,
                    _logger),
                Landing => new LandingLogicCreator(
                    (Landing)flight,
                    (await _routeLogicProvider.GetNextRouteAsync(FlightType.Landing, ct))!,
                    _logger),
                _ => throw new ArgumentException("Unknown type of flight.")
            };
        }
    }
}

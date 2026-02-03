using Airport.Models.Enums;

namespace Airport.Domain.Factories
{
    public class FlightLogicFactory : IFlightLogicFactory
    {
        #region Fields
        private readonly IRouteLogicProvider _routeProvider;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<FlightLogic> _logger;
        #endregion

        public FlightLogicFactory(
            IRouteLogicProvider routeProvider,
            IDomainEvents domainEvents,
            ILogger<FlightLogic> logger)
        {
            _routeProvider = routeProvider;
            _domainEvents = domainEvents;
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
                    (await _routeProvider.GetNextRouteAsync(FlightType.Departure, ct))!,
                    _domainEvents,
                    _logger),
                Landing => new LandingLogicCreator(
                    (Landing)flight,
                    (await _routeProvider.GetNextRouteAsync(FlightType.Landing, ct))!,
                    _domainEvents,
                    _logger),
                _ => throw new ArgumentException("Unknown flight type.")
            };
        }
    }
}

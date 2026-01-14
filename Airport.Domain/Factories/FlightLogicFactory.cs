using Airport.Models.Enums;
using System.Threading;

namespace Airport.Domain.Factories
{
    public class FlightLogicFactory : IFlightLogicFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public FlightLogicFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<IFlightLogicCreator> GetCreatorAsync(Flight flight, CancellationToken cancellationToken = default)
        {
            if (flight is null)
                throw new ArgumentNullException(nameof(flight));

            using var scope = _serviceProvider.CreateScope();
            var logger = scope
                .ServiceProvider
                .GetRequiredService<ILogger<FlightLogic>>();
            var routeLogicProvider = scope
                .ServiceProvider
                .GetRequiredService<IRouteLogicProvider>();

            return flight switch
            {
                Departure => new DepartureLogicCreator(
                    (Departure)flight,
                    (await routeLogicProvider.GetNextRouteAsync(FlightType.Departure, cancellationToken))!,
                    logger),
                Landing => new LandingLogicCreator(
                    (Landing)flight,
                    (await routeLogicProvider.GetNextRouteAsync(FlightType.Landing, cancellationToken))!,
                    logger),
                _ => throw new ArgumentException("Unknown type of flight.")
            };
        }
    }
}

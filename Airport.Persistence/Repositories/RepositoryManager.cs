using Airport.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Airport.Persistence.Repositories
{
    public sealed class RepositoryManager : IRepositoryManager
    {
        #region Fields
        private readonly Lazy<IStationRepository> _lazyStationRepository = null!;
        private readonly Lazy<IRouteRepository> _lazyRouteRepository = null!;
        private readonly Lazy<IFlightRepository> _lazyFlightRepository = null!;
        private readonly Lazy<ITrafficLightRepository> _lazyTrafficLightRepository = null!;
        #endregion

        public RepositoryManager(IServiceProvider serviceProvider)
        {
            var flightRepositoryLogger = serviceProvider.GetRequiredService<ILogger<FlightRepository>>();
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            var dbConfiguration = serviceProvider.GetRequiredService<IOptions<AirportDbConfiguration>>();

            _lazyStationRepository = new Lazy<IStationRepository>(() => new StationRepository(client, dbConfiguration));
            _lazyRouteRepository = new Lazy<IRouteRepository>(() => new RouteRepository(client, dbConfiguration));
            _lazyFlightRepository = new Lazy<IFlightRepository>(() => new FlightRepository(flightRepositoryLogger, client, dbConfiguration));
            _lazyTrafficLightRepository = new Lazy<ITrafficLightRepository>(() => new TrafficLightRepository(client, dbConfiguration));
        }

        #region Properties
        public IStationRepository StationRepository => _lazyStationRepository.Value;
        public IRouteRepository RouteRepository => _lazyRouteRepository.Value;
        public IFlightRepository FlightRepository => _lazyFlightRepository.Value;
        public ITrafficLightRepository TrafficLightRepository => _lazyTrafficLightRepository.Value;
        #endregion

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

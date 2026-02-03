using Airport.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Airport.Persistence.Repositories
{
    public sealed class RepositoryManager : IRepositoryManager
    {
        #region Fields
        private readonly Lazy<IStationRepository> _lazyStationRepository;
        private readonly Lazy<IRouteRepository> _lazyRouteRepository;
        private readonly Lazy<IFlightRepository> _lazyFlightRepository;
        private readonly Lazy<ITrafficLightRepository> _lazyTrafficLightRepository;
        #endregion

        public RepositoryManager(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            var dbConfiguration = serviceProvider.GetRequiredService<IOptions<AirportDbConfiguration>>();

            _lazyStationRepository = new Lazy<IStationRepository>(() => new StationRepository(client, dbConfiguration));
            _lazyRouteRepository = new Lazy<IRouteRepository>(() => new RouteRepository(client, dbConfiguration));
            _lazyFlightRepository = new Lazy<IFlightRepository>(() => new FlightRepository(client, dbConfiguration));
            _lazyTrafficLightRepository = new Lazy<ITrafficLightRepository>(() => new TrafficLightRepository(client, dbConfiguration));
        }

        #region Properties
        public IStationRepository StationRepository => _lazyStationRepository.Value;
        public IRouteRepository RouteRepository => _lazyRouteRepository.Value;
        public IFlightRepository FlightRepository => _lazyFlightRepository.Value;
        public ITrafficLightRepository TrafficLightRepository => _lazyTrafficLightRepository.Value;
        #endregion

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

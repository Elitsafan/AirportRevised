using Airport.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Airport.Persistence.Repositories
{
    public sealed class RepositoryManager : IRepositoryManager
    {
        #region Fields
        private readonly Lazy<IStationRepository> _lazyStationRepository;
        private readonly Lazy<IRouteRepository> _lazyRouteRepository;
        private readonly Lazy<IFlightRepository> _lazyFlightRepository;
        private readonly Lazy<ISectionRepository> _lazySectionRepository;
        private readonly Lazy<ISyncerRepository> _lazySyncerRepository;
        private readonly Lazy<ITrafficLightRepository> _lazyTrafficLightRepository;
        #endregion

        public RepositoryManager(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            var dbConfiguration = serviceProvider.GetRequiredService<IOptions<AirportDbConfiguration>>();

            _lazyStationRepository = new Lazy<IStationRepository>(() => new StationRepository(client, dbConfiguration));
            _lazyRouteRepository = new Lazy<IRouteRepository>(() => new RouteRepository(client, dbConfiguration));
            _lazyFlightRepository = new Lazy<IFlightRepository>(() => new FlightRepository(client, dbConfiguration));
            _lazySectionRepository = new Lazy<ISectionRepository>(() => new SectionRepository(client, dbConfiguration));
            _lazySyncerRepository = new Lazy<ISyncerRepository>(() => new SyncerRepository(client, dbConfiguration));
            _lazyTrafficLightRepository = new Lazy<ITrafficLightRepository>(() => new TrafficLightRepository(client, dbConfiguration));
        }

        #region Properties
        public IStationRepository StationRepository => _lazyStationRepository.Value;
        public IRouteRepository RouteRepository => _lazyRouteRepository.Value;
        public IFlightRepository FlightRepository => _lazyFlightRepository.Value;
        public ISectionRepository SectionRepository => _lazySectionRepository.Value;
        public ISyncerRepository SyncerRepository => _lazySyncerRepository.Value;
        public ITrafficLightRepository TrafficLightRepository => _lazyTrafficLightRepository.Value;
        #endregion
    }
}

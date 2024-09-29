namespace Airport.Domain.Providers
{
    //TODO: Use repository and cache 
    public class StationLogicProvider : IStationLogicProvider
    {
        private IServiceProvider _serviceProvider = null!;
        private HashSet<IStationLogic> _stations = null!;

        private StationLogicProvider()
        {
        }

        public static async Task<StationLogicProvider> CreateAsync(IServiceProvider serviceProvider) =>
            await new StationLogicProvider().InitializeAsync(serviceProvider);

        public async Task<IEnumerable<IStationLogic>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await Task.FromResult(_stations);

        public async Task<IStationLogic> GetStationLogicByIdAsync(
            ObjectId id,
            CancellationToken cancellationToken = default)
        {
            var stationLogic = _stations.FirstOrDefault(s => s.StationId == id);
            return stationLogic is null
                ? throw new LogicNotFoundException()
                : await Task.FromResult(stationLogic);
        }

        public async Task<IEnumerable<IStationLogic>> FindStationLogicsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default)
        {
            var route = await _serviceProvider
                .CreateAsyncScope()
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository
                .GetByIdAsync(routeId, cancellationToken);
            var stationRepository = _serviceProvider
                .CreateAsyncScope()
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .StationRepository;
            try
            {
                return (await stationRepository
                    .GetStationsByRouteAsync(route, cancellationToken))
                    .Join(_stations, s => s.StationId, sl => sl.StationId, (l, r) => r);
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentException("Route not found with that id provided.");
            }
        }

        public async Task<IEnumerable<IStationLogic>> FindTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default)
        {
            var trafficLightRepository = _serviceProvider
                .CreateAsyncScope()
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .TrafficLightRepository;
            var trafficLights = await trafficLightRepository.GetTrafficLightsByRouteIdAsync(routeId, cancellationToken);
            return _stations.Join(
                trafficLights, 
                s => s.StationId, 
                tl => tl.StationId, 
                (s, tl) => s);
        }

        public async Task<IEnumerable<IStationLogic>> FindNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId trafficLightId,
            CancellationToken cancellationToken = default)
        {
            var trafficLightRepository = _serviceProvider
                .CreateAsyncScope()
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .TrafficLightRepository;
            IEnumerable<TrafficLight> nextTrafficLights;
            try
            {
                nextTrafficLights = await trafficLightRepository.GetNextTrafficLightsAsync(
                    routeId,
                    trafficLightId,
                    cancellationToken);
            }
            catch (EntityNotFoundException)
            {
                throw new InvalidOperationException("Route not found. Cannot get the next traffic lights.");
            }
            return _stations.Join(
                nextTrafficLights, 
                s => s.StationId, 
                tl => tl.StationId, 
                (s, tl) => s);
        }

        private async Task<StationLogicProvider> InitializeAsync(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var stationLogicFactory = _serviceProvider.GetRequiredService<IStationLogicFactory>();
            var stationRepository = _serviceProvider
                .CreateAsyncScope()
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .StationRepository;

            // Creates the station logics
            var stations = await stationRepository.GetAllAsync();
            if (!stations.Any())
                throw new InvalidOperationException("There are no stations.");
            _stations = new HashSet<IStationLogic>(
                stations.Select(s => stationLogicFactory
                    .GetCreator(s)
                    .Create()));
            return this;
        }
    }
}

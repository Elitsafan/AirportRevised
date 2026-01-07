using Airport.Domain.Repositories;

namespace Airport.Domain.Providers
{
    //TODO: Use repository and cache 
    public class DirectionLogicProvider : IDirectionLogicProvider
    {
        private IServiceProvider _serviceProvider = null!;
        private HashSet<IDirectionLogic> _directions = null!;

        public static async Task<DirectionLogicProvider> CreateAsync(IServiceProvider serviceProvider) =>
            await new DirectionLogicProvider().InitializeAsync(serviceProvider);

        public async Task<IEnumerable<IDirectionLogic>> GetDirectionsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default)
        {
            var routeRepository = _serviceProvider
                .CreateAsyncScope()
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;
            var route = await routeRepository
                .GetByIdAsync(routeId, cancellationToken) ?? throw new EntityNotFoundException("Route not found.");
            return route.Directions.Join(
                _directions,
                dLeft => new { dLeft.From, dLeft.To },
                dRight => new { dRight.From, dRight.To },
                (l, r) => r);
        }

        private DirectionLogicProvider()
        {
        }

        private async Task<DirectionLogicProvider> InitializeAsync(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var routeRepository = _serviceProvider
                .CreateAsyncScope()
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;
            var directionLogicFactory = _serviceProvider.GetRequiredService<IDirectionLogicFactory>();
            // Creates the direction logics
            _directions = new HashSet<IDirectionLogic>((await routeRepository.GetAllAsync(default))
                .SelectMany(r => r.Directions)
                .Select(d => directionLogicFactory
                    .GetCreator(d)
                    .Create()));
            return this;
        }
    }
}

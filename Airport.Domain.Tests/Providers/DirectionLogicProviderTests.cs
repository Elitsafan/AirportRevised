using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Tests.Providers
{
    public class DirectionLogicProviderTests
    {
        #region Fields
        private IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IRouteRepository> _mockRouteRepository;
        private readonly Mock<IDirectionLogic> _mockDirectionLogic;
        private readonly Mock<IDirectionLogicFactory> _mockDirectionLogicFactory;
        private readonly Mock<IDirectionLogicCreator> _mockDirectionLogicCreator;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly ILogger<DirectionLogicProvider> _mockLogger;
        private IDirectionLogicProvider _directionLogicProvider = null!;
        private Route _route;
        #endregion

        public DirectionLogicProviderTests()
        {
            _mockDomainEvents = new Mock<IDomainEvents>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockDirectionLogic = new Mock<IDirectionLogic>();
            _mockDirectionLogicFactory = new Mock<IDirectionLogicFactory>();
            _mockDirectionLogicCreator = new Mock<IDirectionLogicCreator>();
            _mockLogger = Mock.Of<ILogger<DirectionLogicProvider>>();
            _route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                Directions = new List<Direction>
                {
                    new Direction
                    {
                        From = ObjectId.GenerateNewId(),
                        To = ObjectId.GenerateNewId(),
                    }
                }
            };

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepository.Object);
            _mockRouteRepository
                .Setup(x => x.GetRouteByIdAsync(It.IsAny<ObjectId>(), default))
                .ReturnsAsync(_route);
            _mockRouteRepository
                .Setup(x => x.GetAllAsync(default))
                .ReturnsAsync(new Route[] { _route });
            _mockDirectionLogicFactory
                .Setup(x => x.GetCreator(It.IsAny<Direction>()))
                .Returns(_mockDirectionLogicCreator.Object);
            _mockDirectionLogicCreator
                .Setup(x => x.Create())
                .Returns(_mockDirectionLogic.Object);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IRepositoryManager>(factory => _mockRepositoryManager.Object);
            serviceCollection.AddSingleton<IDirectionLogicFactory>(_mockDirectionLogicFactory.Object);
            serviceCollection.AddSingleton<IDomainEvents>(_mockDomainEvents.Object);
            serviceCollection.AddMemoryCache(options =>
            {
                options.SizeLimit = 1024;
            });
            _serviceProvider = serviceCollection.BuildServiceProvider();
            using var scope = _serviceProvider.CreateScope();
            _cache = scope
                .ServiceProvider
                .GetRequiredService<IMemoryCache>();
        }

        [Fact]
        public async Task GetDirectionsByRouteIdAsync_WhenCalled_ReturnsValueAsync()
        {
            _mockDirectionLogic
                .SetupGet(x => x.From)
                .Returns(_route.Directions[0].From);
            _mockDirectionLogic
                .SetupGet(x => x.To)
                .Returns(_route.Directions[0].To);

            _directionLogicProvider = new DirectionLogicProvider(
                _serviceProvider,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);
            var result = await _directionLogicProvider
                .GetDirectionsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>());

            Assert.Single(result, _mockDirectionLogic.Object);
        }

        [Fact]
        public async Task GetDirectionsByRouteIdAsync_RouteNotExist_ThrowsEntityNotFoundExceptionAsync()
        {
            _mockDirectionLogic
                .SetupGet(x => x.From)
                .Returns(_route.Directions[0].From);
            _mockDirectionLogic
                .SetupGet(x => x.To)
                .Returns(_route.Directions[0].To);
            _mockRouteRepository
                .Setup(x => x.GetRouteByIdAsync(It.IsAny<ObjectId>(), default))
                .ReturnsAsync(It.IsAny<Route>());

            _directionLogicProvider = new DirectionLogicProvider(
                _serviceProvider,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _directionLogicProvider
                .GetDirectionsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()));
        }
    }
}

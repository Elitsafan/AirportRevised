namespace Airport.Domain.Tests.Providers
{
    public class DirectionLogicProviderTests
    {
        #region Fields
        private IServiceProvider _serviceProvider;
        private Mock<IRepositoryManager> _mockRepositoryManager;
        private Mock<IRouteRepository> _mockRouteRepository;
        private Mock<IDirectionLogic> _mockDirectionLogic;
        private Mock<IDirectionLogicFactory> _mockDirectionLogicFactory;
        private Mock<IDirectionLogicCreator> _mockDirectionLogicCreator;
        private Route _route;
        #endregion

        public DirectionLogicProviderTests()
        {
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockDirectionLogic = new Mock<IDirectionLogic>();
            _mockDirectionLogicFactory = new Mock<IDirectionLogicFactory>();
            _mockDirectionLogicCreator = new Mock<IDirectionLogicCreator>();
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
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>(), default))
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
            _serviceProvider = serviceCollection.BuildServiceProvider();
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

            var directionLogicProvider = await DirectionLogicProvider.CreateAsync(_serviceProvider);
            var result = await directionLogicProvider
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
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>(), default))
                .ReturnsAsync(It.IsAny<Route>());

            var directionLogicProvider = await DirectionLogicProvider.CreateAsync(_serviceProvider);
            await Assert.ThrowsAsync<EntityNotFoundException>(() => directionLogicProvider
                .GetDirectionsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()));
        }
    }
}

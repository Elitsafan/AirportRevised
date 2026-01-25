using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Tests.Providers
{
    public class RouteLogicProviderTests
    {
        #region Fields
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IRouteRepository> _mockRouteRepository;
        private readonly MemoryCache _cache;
        private readonly ILogger<RouteLogicProvider> _mockLogger;
        private readonly Mock<IStationLogicProvider> _mockStationLogicProvider;
        private readonly Mock<IRouteLogicFactory> _mockRouteLogicFactory;
        private readonly Mock<IRouteLogicCreator> _mockRouteLogicCreator;
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        #endregion

        public RouteLogicProviderTests()
        {
            _mockLogger = Mock.Of<ILogger<RouteLogicProvider>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
            _mockRouteLogicFactory = new Mock<IRouteLogicFactory>();
            _mockRouteLogicCreator = new Mock<IRouteLogicCreator>();
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockDomainEvents = new Mock<IDomainEvents>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(_mockScopeFactory.Object);
            _mockScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(_mockScope.Object);
            _mockScope
                .Setup(x => x.ServiceProvider)
                .Returns(_mockServiceProvider.Object);
            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IRepositoryManager)))
                .Returns(_mockRepositoryManager.Object);
            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepository.Object);
            _mockRouteLogicFactory
                .Setup(x => x.GetCreator(
                    It.IsAny<Route>(),
                    It.IsAny<IEnumerable<IRouteSectionDetails>>()))
                .Returns(_mockRouteLogicCreator.Object);
            _mockRouteLogicCreator
                .Setup(x => x.CreateAsync())
                .ReturnsAsync(_mockRouteLogic.Object);
        }

        [Fact]
        public async Task GetNextRoute_WhenCalledWithDeparture_ReturnsNextDepartureRoute()
        {
            // Arrange
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                RouteName = "Departure",
                Directions = new List<Direction>
                {
                    new Direction
                    {
                        From = ObjectId.GenerateNewId(),
                        To = ObjectId.GenerateNewId()
                    }
                }
            };
            _mockRouteRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Route> { route });
            _mockRouteLogic
                .SetupGet(x => x.RouteId)
                .Returns(route.RouteId);
            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns(route.RouteName);

            var routeLogicProvider = new RouteLogicProvider(
                _mockServiceProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRouteLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            var result = await routeLogicProvider.GetNextRouteAsync(FlightType.Departure);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(route.RouteId, result.RouteId);
            Assert.Equal(route.RouteName, result.RouteName);
        }

        [Fact]
        public async Task GetNextRoute_WhenCalledWithLanding_ReturnsNextLandingRoute()
        {
            // Arrange
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                RouteName = "Landing",
                Directions = new List<Direction>
                {
                    new Direction
                    {
                        From = ObjectId.GenerateNewId(),
                        To = ObjectId.GenerateNewId()
                    }
                }
            };
            _mockRouteRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Route> { route });
            _mockRouteLogic
                .SetupGet(x => x.RouteId)
                .Returns(route.RouteId);
            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns(route.RouteName);

            var routeLogicProvider = new RouteLogicProvider(
                _mockServiceProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRouteLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            var result = await routeLogicProvider.GetNextRouteAsync(FlightType.Landing);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(route.RouteId, result.RouteId);
            Assert.Equal(route.RouteName, result.RouteName);
        }
    }
}

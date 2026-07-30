using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Tests.Providers
{
    public class RouteLogicProviderTests
    {
        #region Fields
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IRouteRepository> _mockRouteRepository;
        private readonly MemoryCache _cache;
        private readonly ILogger<RouteLogicProvider> _mockLogger;
        private readonly Mock<IStationLogicProvider> _mockStationProvider;
        private readonly Mock<ISectionLogicProvider> _mockSectionProvider;
        private readonly Mock<IRouteLogicFactory> _mockRouteLogicFactory;
        private readonly Mock<IRouteLogicCreator> _mockRouteLogicCreator;
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        #endregion

        public RouteLogicProviderTests()
        {
            _mockLogger = Mock.Of<ILogger<RouteLogicProvider>>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockStationProvider = new Mock<IStationLogicProvider>();
            _mockSectionProvider = new Mock<ISectionLogicProvider>();
            _mockRouteLogicFactory = new Mock<IRouteLogicFactory>();
            _mockRouteLogicCreator = new Mock<IRouteLogicCreator>();
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockDomainEvents = new Mock<IDomainEvents>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepository.Object);
            _mockRouteLogicFactory
                .Setup(x => x.GetCreator(
                    It.IsAny<Route>(),
                    It.IsAny<IEnumerable<ISectionLogic>>(),
                    It.IsAny<IEnumerable<IStationLogic>>()))
                .Returns(_mockRouteLogicCreator.Object);
            _mockRouteLogicCreator
                .Setup(x => x.CreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockRouteLogic.Object);
        }

        [Fact]
        public async Task GetNextRouteAsync_WhenCalledWithDeparture_ReturnsNextDepartureRoute()
        {
            // Arrange
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                RouteName = "Departure"
            };
            var mockSectionLogics = new[]
            {
                new Mock<ISectionLogic>(),
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
            _mockSectionProvider
                .Setup(x => x.GetAllAsync(default))
                .ReturnsAsync(new Dictionary<ObjectId, List<ISectionLogic>>
                {
                    { route.RouteId, mockSectionLogics.Select(s=>s.Object).ToList() }
                }.AsReadOnly());

            var routeLogicProvider = new RouteLogicProvider(
                _mockRepositoryManager.Object,
                _mockStationProvider.Object,
                _mockSectionProvider.Object,
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
        public async Task GetNextRouteAsync_WhenCalledWithLanding_ReturnsNextLandingRoute()
        {
            // Arrange
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                RouteName = "Landing"
            };
            var mockSectionLogics = new[]
            {
                new Mock<ISectionLogic>(),
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
            _mockSectionProvider
                .Setup(x => x.GetAllAsync(default))
                .ReturnsAsync(new Dictionary<ObjectId, List<ISectionLogic>>
                {
                    { route.RouteId, mockSectionLogics.Select(s=>s.Object).ToList() }
                }.AsReadOnly());

            var routeLogicProvider = new RouteLogicProvider(
                _mockRepositoryManager.Object,
                _mockStationProvider.Object,
                _mockSectionProvider.Object,
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

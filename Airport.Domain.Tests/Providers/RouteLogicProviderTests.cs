using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Tests.Providers
{
    public class RouteLogicProviderTests
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RouteLogicProvider> _mockLogger;
        private readonly IMemoryCache _cache;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IRouteRepository> _mockRouteRepository;
        private readonly Mock<IRouteLogicCreator> _mockRouteLogicCreator;
        private readonly Mock<IRouteLogicFactory> _mockRouteLogicFactory;
        private readonly Mock<IStationLogicProvider> _mockStationLogicProvider;
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private IRouteLogicProvider _routeLogicProvider = null!;
        #endregion

        public RouteLogicProviderTests()
        {
            _mockRouteLogicFactory = new Mock<IRouteLogicFactory>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockRouteLogicCreator = new Mock<IRouteLogicCreator>();
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockDomainEvents = new Mock<IDomainEvents>();
            _mockLogger = Mock.Of<ILogger<RouteLogicProvider>>();
            var route = new Route();

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepository.Object);
            _mockRouteRepository
                .Setup(x => x.GetAllAsync(default))
                .ReturnsAsync(new Route[] { route });
            _mockRouteLogicFactory
                .Setup(x => x.GetCreator(route, It.IsAny<IEnumerable<IRouteSectionDetails>>()))
                .Returns(_mockRouteLogicCreator.Object);
            _mockRouteLogicCreator
                .Setup(x => x.CreateAsync())
                .ReturnsAsync(_mockRouteLogic.Object);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IRouteLogicFactory>(_mockRouteLogicFactory.Object);
            serviceCollection.AddSingleton<IStationLogicProvider>(_mockStationLogicProvider.Object);
            serviceCollection.AddSingleton<ILogger<RouteLogicProvider>>(_mockLogger);
            serviceCollection.AddScoped<IRepositoryManager>(factory => _mockRepositoryManager.Object);
            _serviceProvider = serviceCollection.BuildServiceProvider();
            using var scope = _serviceProvider.CreateScope();
            _cache = scope
                .ServiceProvider
                .GetRequiredService<IMemoryCache>();
        }

        [Fact]
        public void RouteLogicProvider_CreatedWithNoRoutes_ThrowsInvalidOperationExceptionAsync() =>
            Assert.Throws<InvalidOperationException>(() =>
                new RouteLogicProvider(
                    _serviceProvider,
                    _cache,
                    _mockDomainEvents.Object,
                    _mockLogger));

        [Fact]
        public void RouteLogicProvider_Created_NotNull()
        {
            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns("Landing");
            _routeLogicProvider = new RouteLogicProvider(
                    _serviceProvider,
                    _cache,
                    _mockDomainEvents.Object,
                    _mockLogger);

            Assert.NotNull(_routeLogicProvider);

            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns("Departure");
            _routeLogicProvider = new RouteLogicProvider(
                    _serviceProvider,
                    _cache,
                    _mockDomainEvents.Object,
                    _mockLogger);

            Assert.NotNull(_routeLogicProvider);
        }

        [Fact]
        public async Task GetNextRoute_WhenCalledWithDeparture_ReturnsNextDepartureRouteAsync()
        {
            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns("Departure");

            _routeLogicProvider = new RouteLogicProvider(
                    _serviceProvider,
                    _cache,
                    _mockDomainEvents.Object,
                    _mockLogger);
            Assert.NotNull(await _routeLogicProvider.GetNextRouteAsync(FlightType.Departure));
        }

        [Fact]
        public void GetNextRoute_WhenCalledWithLanding_ReturnsNextLandingRoute()
        {
            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns("Landing");

            _routeLogicProvider = new RouteLogicProvider(
                    _serviceProvider,
                    _cache,
                    _mockDomainEvents.Object,
                    _mockLogger);

            Assert.NotNull(_routeLogicProvider.GetNextRouteAsync(FlightType.Landing));
        }
    }
}

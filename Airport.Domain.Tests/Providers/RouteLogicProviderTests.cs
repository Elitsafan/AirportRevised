namespace Airport.Domain.Tests.Providers
{
    public class RouteLogicProviderTests
    {
        #region Fields
        private IServiceProvider _serviceProvider;
        private ILogger<RouteLogicProvider> _mockLogger;
        private Mock<IRepositoryManager> _mockRepositoryManager;
        private Mock<IRouteRepository> _mockRouteRepository;
        private Mock<IRouteLogicCreator> _mockRouteLogicCreator;
        private Mock<IRouteLogicFactory> _mockRouteLogicFactory;
        private Mock<IStationLogicProvider> _mockStationLogicProvider;
        private Mock<IRouteLogic> _mockRouteLogic;
        #endregion

        public RouteLogicProviderTests()
        {
            _mockRouteLogicFactory = new Mock<IRouteLogicFactory>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockRouteLogicCreator = new Mock<IRouteLogicCreator>();
            _mockRouteLogic = new Mock<IRouteLogic>();
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
        }

        [Fact]
        public async Task RouteLogicProvider_CreatedWithNoRoutes_ThrowsInvalidOperationExceptionAsync() => 
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => RouteLogicProvider.CreateAsync(_serviceProvider));

        [Fact]
        public async Task RouteLogicProvider_Created_NotNullAsync()
        {
            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns("Landing");
            IRouteLogicProvider routeLogicProvider = await RouteLogicProvider.CreateAsync(_serviceProvider);

            Assert.NotNull(routeLogicProvider);

            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns("Departure");
            routeLogicProvider = await RouteLogicProvider.CreateAsync(_serviceProvider);

            Assert.NotNull(routeLogicProvider);
        }

        [Fact]
        public async Task GetNextRoute_WhenCalledWithDeparture_ReturnsNextDepartureRouteAsync()
        {
            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns("Departure");

            IRouteLogicProvider routeLogicProvider = await RouteLogicProvider.CreateAsync(_serviceProvider);
            Assert.NotNull(routeLogicProvider.GetNextRoute(FlightType.Departure));
        }

        [Fact]
        public async Task GetNextRoute_WhenCalledWithLanding_ReturnsNextLandingRouteAsync()
        {
            _mockRouteLogic
                .SetupGet(x => x.RouteName)
                .Returns("Landing");

            IRouteLogicProvider routeLogicProvider = await RouteLogicProvider.CreateAsync(_serviceProvider);
            Assert.NotNull(routeLogicProvider.GetNextRoute(FlightType.Landing));
        }
    }
}

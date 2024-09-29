namespace Airport.Domain.Tests.Factories
{
    public class RouteLogicFactoryTests
    {
        #region Fields
        private ServiceProvider _serviceProvider;
        private ILogger<RouteLogic> _mockLogger;
        private Mock<IDirectionLogicProvider> _mockDirectionLogicProvider;
        private Mock<IStationLogicProvider> _mockStationLogicProvider;
        #endregion

        public RouteLogicFactoryTests()
        {
            _mockLogger = Mock.Of<ILogger<RouteLogic>>();
            _mockDirectionLogicProvider = new Mock<IDirectionLogicProvider>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILogger<RouteLogic>>(_mockLogger);
            serviceCollection.AddSingleton<IDirectionLogicProvider>(_mockDirectionLogicProvider.Object);
            serviceCollection.AddSingleton<IStationLogicProvider>(_mockStationLogicProvider.Object);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void GetCreator_WhenCalled_ReturnsNotNull()
        {
            IRouteLogicFactory routeLogicFactory = new RouteLogicFactory(_serviceProvider);
            var mockCollection = new IRouteSectionDetails[]
            {
                new Mock<IRouteSectionDetails>().Object,
            };
            var creator = routeLogicFactory.GetCreator(new Route(), mockCollection);

            Assert.NotNull(creator);
        }
    }
}

namespace Airport.Domain.Tests.Factories
{
    public class FlightLogicFactoryTests
    {
        #region Fields
        private ServiceProvider _serviceProvider;
        private ILogger<FlightLogic> _mockFlightLogicLogger; 
        private Mock<IRouteLogicFactory> _mockRouteLogicFactory;
        private Mock<IRouteLogicProvider> _mockRouteLogicProvider;
        #endregion

        public FlightLogicFactoryTests()
        {
            _mockRouteLogicFactory = new Mock<IRouteLogicFactory>();
            _mockRouteLogicProvider = new Mock<IRouteLogicProvider>();
            _mockFlightLogicLogger = Mock.Of<ILogger<FlightLogic>>();

            _mockRouteLogicProvider
                .SetupGet(x => x.DepartureRoutes)
                .Returns(new List<IRouteLogic>
                {
                    Mock.Of<IRouteLogic>(),
                });
            _mockRouteLogicProvider
                .SetupGet(x => x.LandingRoutes)
                .Returns(new List<IRouteLogic>
                {
                    Mock.Of<IRouteLogic>(),
                });

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IRouteLogicFactory>(_mockRouteLogicFactory.Object);
            serviceCollection.AddSingleton<IRouteLogicProvider>(_mockRouteLogicProvider.Object);
            serviceCollection.AddSingleton<ILogger<FlightLogic>>(_mockFlightLogicLogger);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void GetCreator_WhenCalled_ReturnsDepartureLogicCreator()
        {
            IFlightLogicFactory flightLogicFactory = new FlightLogicFactory(_serviceProvider);
            IFlightLogicCreator creator = flightLogicFactory.GetCreator(new Departure());

            Assert.NotNull(creator);
            Assert.IsAssignableFrom<DepartureLogicCreator>(creator);
        }

        [Fact]
        public void GetCreator_WhenCalled_ReturnsLandingLogicCreator()
        {
            IFlightLogicFactory flightLogicFactory = new FlightLogicFactory(_serviceProvider);
            IFlightLogicCreator creator = flightLogicFactory.GetCreator(new Landing());

            Assert.NotNull(creator);
            Assert.IsAssignableFrom<LandingLogicCreator>(creator);
        }
    }
}

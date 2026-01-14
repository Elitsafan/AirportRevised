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
                .Setup(x => x.GetDepartureRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IRouteLogic>
                {
                    Mock.Of<IRouteLogic>(),
                });
            _mockRouteLogicProvider
                .Setup(x => x.GetLandingRoutesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IRouteLogic>
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
        public async Task GetCreator_WhenCalled_ReturnsDepartureLogicCreatorAsync()
        {
            IFlightLogicFactory flightLogicFactory = new FlightLogicFactory(_serviceProvider);
            IFlightLogicCreator creator = await flightLogicFactory.GetCreatorAsync(new Departure());

            Assert.NotNull(creator);
            Assert.IsAssignableFrom<DepartureLogicCreator>(creator);
        }

        [Fact]
        public async Task GetCreator_WhenCalled_ReturnsLandingLogicCreatorAsync()
        {
            IFlightLogicFactory flightLogicFactory = new FlightLogicFactory(_serviceProvider);
            IFlightLogicCreator creator = await flightLogicFactory.GetCreatorAsync(new Landing());

            Assert.NotNull(creator);
            Assert.IsAssignableFrom<LandingLogicCreator>(creator);
        }
    }
}

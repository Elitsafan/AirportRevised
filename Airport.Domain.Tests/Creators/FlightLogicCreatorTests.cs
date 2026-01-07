namespace Airport.Domain.Tests.Creators
{
    public class FlightLogicCreatorTests
    {
        #region Fields
        private Mock<IRouteLogicFactory> _mockRouteLogicFactory;
        private Mock<IRouteLogicCreator> _mockRouteLogicCreator;
        private Mock<IRouteLogic> _mockRouteLogic;
        private ILogger<FlightLogic> _mockFlightLogicLogger;
        #endregion

        public FlightLogicCreatorTests()
        {
            _mockRouteLogicFactory = new Mock<IRouteLogicFactory>();
            _mockRouteLogicCreator = new Mock<IRouteLogicCreator>();
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockFlightLogicLogger = Mock.Of<ILogger<FlightLogic>>();

            _mockRouteLogicFactory
                .Setup(x => x.GetCreator(It.IsAny<Route>(), It.IsAny<IEnumerable<IRouteSectionDetails>>()))
                .Returns(_mockRouteLogicCreator.Object);
            _mockRouteLogicCreator
                .Setup(x => x.CreateAsync())
                .ReturnsAsync(_mockRouteLogic.Object);
        }

        [Fact]
        public async Task CreateDepartureLogic_ReturnsNotNullAsync()
        {
            IFlightLogicCreator creator = new DepartureLogicCreator(
                new Departure(),
                _mockRouteLogic.Object,
                _mockFlightLogicLogger);

            var departureLogic = await creator.CreateAsync();
            Assert.NotNull(departureLogic);
        }

        [Fact]
        public async Task CreateLandingLogic_ReturnsNotNullAsync()
        {
            IFlightLogicCreator creator = new LandingLogicCreator(
                new Landing(),
                _mockRouteLogic.Object,
                _mockFlightLogicLogger);

            var landingLogic = await creator.CreateAsync();
            Assert.NotNull(landingLogic);
        }
    }
}
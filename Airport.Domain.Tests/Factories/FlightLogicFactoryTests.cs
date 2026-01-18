namespace Airport.Domain.Tests.Factories
{
    public class FlightLogicFactoryTests
    {
        #region Fields
        private readonly ILogger<FlightLogic> _mockFlightLogicLogger;
        private readonly Mock<IRouteLogicProvider> _mockRouteLogicProvider;
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private IFlightLogicFactory _flightLogicFactory = null!;
        #endregion

        public FlightLogicFactoryTests()
        {
            _mockRouteLogicProvider = new Mock<IRouteLogicProvider>();
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockFlightLogicLogger = Mock.Of<ILogger<FlightLogic>>();
        }

        [Fact]
        public async Task GetCreatorAsync_WhenCalledWithDeparture_ReturnsDepartureLogicCreatorWithCorrectValues()
        {
            // Arrange
            var departure = new Departure
            {
                FlightId = ObjectId.GenerateNewId(),
                RouteId = ObjectId.GenerateNewId(),
                OccupationDetails = new List<OccupationDetails>()
                 {
                     new OccupationDetails
                     {
                         StationId = ObjectId.GenerateNewId(),
                         Entrance = new DateTime(123),
                         Exit = new DateTime(456)
                     }
                 }
            };

            _mockRouteLogicProvider
                .Setup(x => x.GetNextRouteAsync(FlightType.Departure, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockRouteLogic.Object);
            _mockRouteLogic
                .SetupGet(x => x.RouteId)
                .Returns(departure.RouteId.Value);
            _flightLogicFactory = new FlightLogicFactory(
                _mockRouteLogicProvider.Object,
                _mockFlightLogicLogger);
            // Act
            var creator = await _flightLogicFactory.GetCreatorAsync(departure);

            // Assert
            var flightLogic = Assert.IsType<FlightLogic>(await creator.CreateAsync());
            Assert.IsAssignableFrom<DepartureLogicCreator>(creator);
            Assert.Equal(departure.FlightId, flightLogic.FlightId);
            Assert.Equal(departure.RouteId, flightLogic.RouteId);
        }

        [Fact]
        public async Task GetCreatorAsync_WhenCalledWithLanding_ReturnsLandingLogicCreatorWithCorrectValues()
        {
            // Arrange
            var landing = new Landing
            {
                FlightId = ObjectId.GenerateNewId(),
                RouteId = ObjectId.GenerateNewId(),
                OccupationDetails = new List<OccupationDetails>()
                 {
                     new OccupationDetails
                     {
                         StationId = ObjectId.GenerateNewId(),
                         Entrance = new DateTime(123),
                         Exit = new DateTime(456)
                     }
                 }
            };

            _mockRouteLogicProvider
                .Setup(x => x.GetNextRouteAsync(FlightType.Landing, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockRouteLogic.Object);
            _mockRouteLogic
                .SetupGet(x => x.RouteId)
                .Returns(landing.RouteId.Value);
            _flightLogicFactory = new FlightLogicFactory(
                _mockRouteLogicProvider.Object,
                _mockFlightLogicLogger);

            // Act
            var creator = await _flightLogicFactory.GetCreatorAsync(landing);

            // Assert
            var flightLogic = Assert.IsType<FlightLogic>(await creator.CreateAsync());
            Assert.IsAssignableFrom<LandingLogicCreator>(creator);
            Assert.Equal(landing.FlightId, flightLogic.FlightId);
            Assert.Equal(landing.RouteId, flightLogic.RouteId);
        }

        [Fact]
        public async Task GetCreatorAsync_FlightIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            _flightLogicFactory = new FlightLogicFactory(
                _mockRouteLogicProvider.Object,
                _mockFlightLogicLogger);

            // Assert
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(
                () => _flightLogicFactory.GetCreatorAsync(null!));
            Assert.Equal("flight", ex.ParamName);
        }
    }
}

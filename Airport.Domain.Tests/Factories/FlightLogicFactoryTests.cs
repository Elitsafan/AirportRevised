namespace Airport.Domain.Tests.Factories
{
    public class FlightLogicFactoryTests
    {
        #region Fields
        private readonly ILogger<FlightLogic> _mockFlightLogicLogger;
        private readonly Mock<IRouteLogicProvider> _mockRouteLogicProvider;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private IFlightLogicFactory _sut = null!;
        #endregion

        public FlightLogicFactoryTests()
        {
            _mockRouteLogicProvider = new Mock<IRouteLogicProvider>();
            _mockDomainEvents = new Mock<IDomainEvents>();
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

            _sut = new FlightLogicFactory(
                _mockRouteLogicProvider.Object,
                _mockDomainEvents.Object,
                _mockFlightLogicLogger);

            // Act
            var creator = await _sut.GetCreatorAsync(departure);

            // Assert
            var flightLogic = Assert.IsType<FlightLogic>(creator.Create());
            Assert.IsType<DepartureLogicCreator>(creator, exactMatch: false);
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

            _sut = new FlightLogicFactory(
                _mockRouteLogicProvider.Object,
                _mockDomainEvents.Object,
                _mockFlightLogicLogger);

            // Act
            var creator = await _sut.GetCreatorAsync(landing);

            // Assert
            var flightLogic = Assert.IsType<FlightLogic>(creator.Create());
            Assert.IsType<LandingLogicCreator>(creator, exactMatch: false);
            Assert.Equal(landing.FlightId, flightLogic.FlightId);
            Assert.Equal(landing.RouteId, flightLogic.RouteId);
        }

        [Fact]
        public async Task GetCreatorAsync_FlightIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            _sut = new FlightLogicFactory(
                _mockRouteLogicProvider.Object,
                _mockDomainEvents.Object,
                _mockFlightLogicLogger);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetCreatorAsync(null!));

            Assert.Equal("flight", ex.ParamName);
        }
    }
}

namespace Airport.Domain.Tests.Creators
{
    public class FlightLogicCreatorTests
    {
        #region Fields
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private readonly ILogger<FlightLogic> _mockFlightLogicLogger;
        private IFlightLogicCreator _landingLogicCreator = null!;
        private IFlightLogicCreator _departureLogicCreator = null!;
        #endregion

        public FlightLogicCreatorTests()
        {
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockFlightLogicLogger = Mock.Of<ILogger<FlightLogic>>();
        }

        [Fact]
        public async Task CreateDepartureLogic_WhenCalled_ReturnsDepartureLogic()
        {
            // Arrange
            var departure = new Departure
            {
                FlightId = ObjectId.GenerateNewId(),
                RouteId = ObjectId.GenerateNewId(),
                OccupationDetails = new List<OccupationDetails>
                {
                    new OccupationDetails
                    {
                        Entrance = new DateTime(12345),
                        Exit = new DateTime(23456),
                        StationId = ObjectId.GenerateNewId()
                    },
                },
            };
            _mockRouteLogic
                .SetupGet(x => x.RouteId)
                .Returns(departure.RouteId.Value);
            _departureLogicCreator = new DepartureLogicCreator(
                departure,
                _mockRouteLogic.Object,
                _mockFlightLogicLogger);

            // Act
            var result = await _departureLogicCreator.CreateAsync();

            // Assert
            var flightLogic = Assert.IsType<FlightLogic>(result);
            Assert.Equal(departure.RouteId, flightLogic.RouteId);
            Assert.Equal(departure.FlightId, flightLogic.FlightId);
        }

        [Fact]
        public async Task CreateLandingLogic_WhenCalled_ReturnsLandingLogic()
        {
            // Arrange
            var landing = new Landing
            {
                FlightId = ObjectId.GenerateNewId(),
                RouteId = ObjectId.GenerateNewId(),
                OccupationDetails = new List<OccupationDetails>
                {
                    new OccupationDetails
                    {
                        Entrance = new DateTime(12345),
                        Exit = new DateTime(23456),
                        StationId = ObjectId.GenerateNewId()
                    },
                },
            };
            _mockRouteLogic
                .SetupGet(x => x.RouteId)
                .Returns(landing.RouteId.Value);
            _landingLogicCreator = new LandingLogicCreator(
                landing,
                _mockRouteLogic.Object,
                _mockFlightLogicLogger);

            // Act
            var result = await _landingLogicCreator.CreateAsync();

            // Assert
            var flightLogic = Assert.IsType<FlightLogic>(result);
            Assert.Equal(landing.RouteId, flightLogic.RouteId);
            Assert.Equal(landing.FlightId, flightLogic.FlightId);
        }
    }
}
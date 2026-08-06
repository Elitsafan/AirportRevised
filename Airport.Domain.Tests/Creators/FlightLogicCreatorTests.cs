namespace Airport.Domain.Tests.Creators
{
    public class FlightLogicCreatorTests
    {
        #region Fields
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly ILogger<FlightLogic> _mockFlightLogicLogger;
        private IFlightLogicCreator _landingLogicCreator = null!;
        private IFlightLogicCreator _departureLogicCreator = null!;
        #endregion

        public FlightLogicCreatorTests()
        {
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockDomainEvents = new Mock<IDomainEvents>();
            _mockFlightLogicLogger = Mock.Of<ILogger<FlightLogic>>();
        }

        [Fact]
        public void CreateDepartureLogic_WhenCalled_ReturnsDepartureLogic()
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
                _mockDomainEvents.Object,
                _mockFlightLogicLogger);

            // Act
            var result = _departureLogicCreator.Create();

            // Assert
            var flightLogic = Assert.IsType<FlightLogic>(result);
            Assert.Equal(departure.RouteId, flightLogic.RouteId);
            Assert.Equal(departure.FlightId, flightLogic.FlightId);
        }

        [Fact]
        public void CreateLandingLogic_WhenCalled_ReturnsLandingLogic()
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
                _mockDomainEvents.Object,
                _mockFlightLogicLogger);

            // Act
            var result = _landingLogicCreator.Create();

            // Assert
            var flightLogic = Assert.IsType<FlightLogic>(result);
            Assert.Equal(landing.RouteId, flightLogic.RouteId);
            Assert.Equal(landing.FlightId, flightLogic.FlightId);
        }
    }
}
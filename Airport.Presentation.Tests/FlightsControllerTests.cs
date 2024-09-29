namespace Airport.Presentation.Tests
{
    public class FlightsControllerTests
    {
        #region Fields
        private FlightsController _flightsController;
        private Mock<IFlightService> _mockFlightService;
        private Mock<IAirportService> _mockAirportService;
        private ILogger<FlightsController> _mockLogger; 
        #endregion

        public FlightsControllerTests()
        {
            _mockLogger = Mock.Of<ILogger<FlightsController>>();
            _mockFlightService = new Mock<IFlightService>();
            _mockAirportService = new Mock<IAirportService>();
            _flightsController = new FlightsController(_mockFlightService.Object);
        }

        [Fact]
        public void Created_NotNull() => Assert.NotNull(_flightsController);

        [Fact]
        public async Task LandingAsync_WhenCalled_Returns201Async()
        {
            var expected = _flightsController.CreatedAtRoute(
                nameof(AirportController.StatusAsync), 
                StatusCodes.Status201Created);

            var actual = await _flightsController.LandingAsync(
                It.IsAny<ObjectId>(),
                new LandingForCreationDTO(),
                default);

            Assert.Equal(expected.StatusCode, (actual as CreatedAtRouteResult)!.StatusCode);
        }

        [Fact]
        public async Task DepartureAsync_WhenCalled_Returns201Async()
        {
            var expected = _flightsController.CreatedAtRoute(
                nameof(AirportController.StatusAsync),
                StatusCodes.Status201Created);

            var actual = await _flightsController.DepartureAsync(
                It.IsAny<ObjectId>(),
                new DepartureForCreationDTO(),
                default);

            Assert.Equal(expected.StatusCode, (actual as CreatedAtRouteResult)!.StatusCode);
        }
    }
}

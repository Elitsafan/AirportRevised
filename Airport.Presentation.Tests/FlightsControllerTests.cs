using Microsoft.VisualStudio.Threading;

namespace Airport.Presentation.Tests
{
    public class FlightsControllerTests
    {
        #region Fields
        private readonly FlightsController _flightsController;
        private readonly Mock<IFlightService> _mockFlightService;
        #endregion

        public FlightsControllerTests()
        {
            _mockFlightService = new Mock<IFlightService>();
            _flightsController = new FlightsController(_mockFlightService.Object);
        }

        [Fact]
        public void Created_NotNull() => Assert.NotNull(_flightsController);

        [Fact]
        public async Task GetAllFlightsAsync_WhenCalled_ReturnsAllFlights()
        {
            var flights = new FlightDTO[]
            {
                new DepartureDTO(),
                new LandingDTO()
            };

            _mockFlightService
                .Setup(x => x.GetAllFlightsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(flights.ToAsyncEnumerable());

            var result = await _flightsController
                .GetAllFlightsAsync(null)
                .ToListAsync();

            var okResult = Assert.IsType<List<FlightDTO>>(result);
            Assert.Equal(flights, okResult);
        }

        [Fact]
        public async Task LandingAsync_WhenCalled_Returns201()
        {
            var flightForCreationDto = new LandingForCreationDTO();
            var id = ObjectId.GenerateNewId();

            _mockFlightService
                .Setup(x => x.ProcessFlightAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<LandingForCreationDTO>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _flightsController.LandingAsync(
                id,
                flightForCreationDto,
                It.IsAny<CancellationToken>());

            var createdResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(id, createdResult.RouteValues["id"]);
            Assert.Equal(flightForCreationDto, createdResult.Value);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        }

        [Fact]
        public async Task DepartureAsync_WhenCalled_Returns201()
        {
            var flightForCreationDto = new DepartureForCreationDTO();
            var id = ObjectId.GenerateNewId();

            _mockFlightService
                .Setup(x => x.ProcessFlightAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<DepartureForCreationDTO>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _flightsController.DepartureAsync(
                id,
                flightForCreationDto,
                It.IsAny<CancellationToken>());

            var createdResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(id, createdResult.RouteValues["id"]);
            Assert.Equal(flightForCreationDto, createdResult.Value);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        }

        [Fact]
        public async Task GetFlightByIdAsync_WhenCalled_Returns200WithFlightRequested()
        {
            var flightDto = new DepartureDTO { FlightId = ObjectId.GenerateNewId() };

            _mockFlightService
                .Setup(x => x.GetFlightByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(flightDto);

            var result = await _flightsController.GetFlightByIdAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal(flightDto.FlightId, (okResult.Value as FlightDTO)!.FlightId);
        }

        [Fact]
        public async Task GetFlightByIdAsync_FlightNotExists_Returns404()
        {
            _mockFlightService
                .Setup(x => x.GetFlightByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(FlightDTO?));

            var result = await _flightsController.GetFlightByIdAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            var okResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, okResult.StatusCode);
        }

        [Fact]
        public async Task DeleteFlightAsync_WhenCalled_Returns204()
        {
            _mockFlightService
                .Setup(x => x.DeleteFlightAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _flightsController.DeleteFlightAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteFlightAsync_FlightNotExists_Returns404()
        {
            _mockFlightService
                .Setup(x => x.DeleteFlightAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _flightsController.DeleteFlightAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            Assert.IsType<NotFoundResult>(result);
        }
    }
}

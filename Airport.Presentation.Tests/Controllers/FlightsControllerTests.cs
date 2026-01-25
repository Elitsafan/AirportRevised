using Microsoft.VisualStudio.Threading;

namespace Airport.Presentation.Tests.Controllers
{
    public class FlightsControllerTests
    {
        private readonly Mock<IFlightService> _mockFlightService;

        public FlightsControllerTests() => _mockFlightService = new Mock<IFlightService>();

        [Fact]
        public async Task GetAllFlightsAsync_WhenCalled_ReturnsAllFlights()
        {
            // Arrange
            var flights = new FlightDTO[]
            {
                new DepartureDTO(),
                new LandingDTO()
            };

            _mockFlightService
                .Setup(x => x.GetAllFlightsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(flights.ToAsyncEnumerable());
            var flightsController = new FlightsController(_mockFlightService.Object);

            // Act & Assert
            var result = flightsController.GetAllFlightsAsync(null);
            await foreach (var flight in result)
                Assert.Contains(flight, flights);
        }

        [Fact]
        public async Task AddLandingAsync_WhenCalled_Returns201()
        {
            // Arrange
            var flightForCreationDto = new LandingForCreationDTO();
            var id = ObjectId.GenerateNewId();
            var flightDto = new LandingDTO { FlightId = id };

            _mockFlightService
                .Setup(x => x.AddFlightAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<LandingForCreationDTO>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(flightDto);
            var flightsController = new FlightsController(_mockFlightService.Object);

            // Act
            var result = await flightsController.AddLandingAsync(id, flightForCreationDto);

            // Assert
            _mockFlightService.Verify(x => x.AddFlightAsync(
                id,
                flightForCreationDto,
                It.IsAny<CancellationToken>()), Times.Once);
            var createdResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(id, createdResult.RouteValues["id"]);
            Assert.Equal(flightDto, createdResult.Value);
        }

        [Fact]
        public async Task AddDepartureAsync_WhenCalled_Returns201()
        {
            // Arrange
            var flightForCreationDto = new DepartureForCreationDTO();
            var id = ObjectId.GenerateNewId();
            var flightDto = new DepartureDTO { FlightId = id };

            _mockFlightService
                .Setup(x => x.AddFlightAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<DepartureForCreationDTO>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(flightDto);
            var flightsController = new FlightsController(_mockFlightService.Object);

            // Act
            var result = await flightsController.AddDepartureAsync(id, flightForCreationDto);

            // Assert
            _mockFlightService.Verify(x => x.AddFlightAsync(
                id,
                flightForCreationDto,
                It.IsAny<CancellationToken>()), Times.Once);
            var createdResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(id, createdResult.RouteValues["id"]);
            Assert.Equal(flightDto, createdResult.Value);
        }

        [Fact]
        public async Task GetFlightByIdAsync_WhenCalled_Returns200WithFlightRequested()
        {
            // Arrange
            var flightDto = new DepartureDTO { FlightId = ObjectId.GenerateNewId() };

            _mockFlightService
                .Setup(x => x.GetFlightByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(flightDto);
            var flightsController = new FlightsController(_mockFlightService.Object);

            // Act
            var result = await flightsController.GetFlightByIdAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal(flightDto.FlightId, (okResult.Value as FlightDTO)!.FlightId);
        }

        [Fact]
        public async Task GetFlightByIdAsync_FlightNotExists_Returns404()
        {
            // Arrange
            _mockFlightService
                .Setup(x => x.GetFlightByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(FlightDTO?));
            var flightsController = new FlightsController(_mockFlightService.Object);

            // Act
            var result = await flightsController.GetFlightByIdAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteFlightAsync_WhenCalled_Returns204()
        {
            // Arrange
            _mockFlightService
                .Setup(x => x.DeleteFlightAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var flightsController = new FlightsController(_mockFlightService.Object);

            // Act
            var result = await flightsController.DeleteFlightAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteFlightAsync_FlightNotExists_Returns404()
        {
            // Arrange
            _mockFlightService
                .Setup(x => x.DeleteFlightAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var flightsController = new FlightsController(_mockFlightService.Object);

            // Act
            var result = await flightsController.DeleteFlightAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

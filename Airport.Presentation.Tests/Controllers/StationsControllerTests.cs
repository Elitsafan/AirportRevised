namespace Airport.Presentation.Tests.Controllers
{
    public class StationsControllerTests
    {
        private readonly Mock<IStationService> _mockStationService;

        public StationsControllerTests() => _mockStationService = new Mock<IStationService>();

        [Fact]
        public async Task GetAllStationsAsync_WhenCalled_ReturnsAllStations()
        {
            // Arrange
            var stations = new StationDTO[]
            {
                new StationDTO(),
                new StationDTO()
            };

            _mockStationService
                .Setup(x => x.GetAllStationsAsync(It.IsAny<CancellationToken>()))
                .Returns(stations.ToAsyncEnumerable());
            var stationsController = new StationsController(_mockStationService.Object);

            // Act & Assert
            await foreach (var station in stationsController.GetAllStationsAsync())
                Assert.Contains(station, stations);
        }

        [Fact]
        public async Task GetStationByIdAsync_WhenCalled_Returns200WithStationRequested()
        {
            // Arrange
            var stationDto = new StationDTO();
            stationDto.StationId = ObjectId.GenerateNewId();

            _mockStationService
                .Setup(x => x.GetStationByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(stationDto);
            var stationsController = new StationsController(_mockStationService.Object);

            // Act
            var result = await stationsController.GetStationByIdAsync(stationDto.StationId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(stationDto, okResult.Value);
            _mockStationService.Verify(
                x => x.GetStationByIdAsync(stationDto.StationId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetStationByIdAsync_StationNotExists_Returns404()
        {
            // Arrange
            var stationId = ObjectId.GenerateNewId();
            _mockStationService
                .Setup(x => x.GetStationByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(StationDTO)!);
            var stationsController = new StationsController(_mockStationService.Object);

            // Act
            var result = await stationsController.GetStationByIdAsync(stationId);

            // Assert
            var okResult = Assert.IsType<NotFoundResult>(result);
            _mockStationService.Verify(
                x => x.GetStationByIdAsync(stationId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddStationAsync_WhenCalled_Returns201()
        {
            // Arrange
            var routeName = "StationById";
            var stationForCreationDto = new StationForCreationDTO
            {
                EstimatedWaitingTime = TimeSpan.FromSeconds(100),
            };
            var id = ObjectId.GenerateNewId();
            var stationDto = new StationDTO
            {
                StationId = id,
                WaitingTime = stationForCreationDto.EstimatedWaitingTime
            };
            _mockStationService
                .Setup(x => x.AddStationAsync(
                    stationForCreationDto,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(stationDto);
            var stationsController = new StationsController(_mockStationService.Object);

            // Act
            var actual = await stationsController.AddStationAsync(stationForCreationDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtRouteResult>(actual);
            Assert.Equal(stationDto, createdResult.Value);
            Assert.Equal(routeName, createdResult.RouteName);
            _mockStationService.Verify(x => x.AddStationAsync(
                stationForCreationDto,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStationAsync_ValidInput_Returns204()
        {
            // Arrange
            var stationForUpdateDto = new StationForUpdateDTO
            {
                EstimatedWaitingTime = TimeSpan.FromSeconds(100),
            };
            var stationId = ObjectId.GenerateNewId();
            _mockStationService
                .Setup(x => x.UpdateStationAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<StationForUpdateDTO>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(UpdateResult.Modified);

            var stationsController = new StationsController(_mockStationService.Object);

            // Act
            var actual = await stationsController.UpdateStationAsync(stationId, stationForUpdateDto);

            // Assert
            Assert.IsType<NoContentResult>(actual);
        }

        [Fact]
        public async Task UpdateStationAsync_StationNotFound_Returns400()
        {
            // Arrange
            var stationId = ObjectId.GenerateNewId();
            _mockStationService
                .Setup(x => x.UpdateStationAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<StationForUpdateDTO>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(UpdateResult.Failed);

            var stationsController = new StationsController(_mockStationService.Object);

            // Act
            var actual = await stationsController.UpdateStationAsync(stationId, new StationForUpdateDTO());

            // Assert
            Assert.IsType<NotFoundResult>(actual);
        }

        [Fact]
        public async Task DeleteStationAsync_WhenCalled_Returns204()
        {
            // Arrange
            _mockStationService
                .Setup(x => x.DeleteStationAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var stationsController = new StationsController(_mockStationService.Object);

            // Act
            var result = await stationsController.DeleteStationAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteStationAsync_StationNotExists_Returns404()
        {
            // Arrange
            _mockStationService
                .Setup(x => x.DeleteStationAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var stationsController = new StationsController(_mockStationService.Object);

            // Act
            var result = await stationsController.DeleteStationAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

namespace Airport.Presentation.Tests.Controllers
{
    public class RoutesControllerTests
    {
        private readonly Mock<IRouteService> _mockRouteService;

        public RoutesControllerTests() => _mockRouteService = new Mock<IRouteService>();

        [Fact]
        public async Task GetAllRoutesAsync_WhenCalled_ReturnsAllRoutes()
        {
            // Arrange
            var routes = new RouteDTO[]
            {
                new RouteDTO(),
                new RouteDTO()
            };

            _mockRouteService
                .Setup(x => x.GetAllRoutesAsync(It.IsAny<CancellationToken>()))
                .Returns(routes.ToAsyncEnumerable());
            var routesController = new RoutesController(_mockRouteService.Object);

            // Act & Assert
            await foreach (var route in routesController.GetAllRoutesAsync())
                Assert.Contains(route, routes);
        }

        [Fact]
        public async Task GetRouteByIdAsync_WhenCalled_Returns200WithRouteRequested()
        {
            // Arrange
            var routeDto = new RouteDTO();
            routeDto.RouteId = ObjectId.GenerateNewId();

            _mockRouteService
                .Setup(x => x.GetRouteByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(routeDto);
            var routesController = new RoutesController(_mockRouteService.Object);

            // Act
            var result = await routesController.GetRouteByIdAsync(routeDto.RouteId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(routeDto, okResult.Value);
            _mockRouteService.Verify(
                x => x.GetRouteByIdAsync(routeDto.RouteId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetRouteByIdAsync_RouteNotExists_Returns404()
        {
            // Arrange
            var routeId = ObjectId.GenerateNewId();
            _mockRouteService
                .Setup(x => x.GetRouteByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(RouteDTO)!);
            var routesController = new RoutesController(_mockRouteService.Object);

            // Act
            var result = await routesController.GetRouteByIdAsync(routeId);

            // Assert
            var okResult = Assert.IsType<NotFoundResult>(result);
            _mockRouteService.Verify(
                x => x.GetRouteByIdAsync(routeId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddRouteAsync_WhenCalled_Returns201()
        {
            // Arrange
            var routeName = "RouteById";
            var routeForCreationDto = new RouteForCreationDTO
            {
                RouteName = "TestRoute",
                Directions = new List<DirectionDTO>()
                {
                    new()
                    {
                        From = ObjectId.GenerateNewId(),
                        To = ObjectId.GenerateNewId()
                    }
                }
            };
            var id = ObjectId.GenerateNewId();
            var routeDto = new RouteDTO
            {
                RouteId = id,
                RouteName = routeForCreationDto.RouteName,
                Directions = routeForCreationDto.Directions,
            };
            _mockRouteService
                .Setup(x => x.AddRouteAsync(
                    routeForCreationDto,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(routeDto);
            var routesController = new RoutesController(_mockRouteService.Object);

            // Act
            var actual = await routesController.AddRouteAsync(routeForCreationDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtRouteResult>(actual);
            Assert.Equal(routeDto, createdResult.Value);
            Assert.Equal(routeName, createdResult.RouteName);
            _mockRouteService.Verify(x => x.AddRouteAsync(
                routeForCreationDto,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateRouteAsync_ValidInput_Returns204()
        {
            // Arrange
            var routeForUpdateDto = new RouteForUpdateDTO
            {
                RouteName = "TestRoute",
                Directions = new List<DirectionDTO>()
                {
                    new()
                    {
                        From = ObjectId.GenerateNewId(),
                        To = ObjectId.GenerateNewId()
                    }
                }
            };
            var routeId = ObjectId.GenerateNewId();
            _mockRouteService
                .Setup(x => x.UpdateRouteAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<RouteForUpdateDTO>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(UpdateResult.Modified);

            var routesController = new RoutesController(_mockRouteService.Object);

            // Act
            var actual = await routesController.UpdateRouteAsync(routeId, routeForUpdateDto);

            // Assert
            Assert.IsType<NoContentResult>(actual);
        }

        [Fact]
        public async Task UpdateRouteAsync_RouteNotFound_Returns400()
        {
            // Arrange
            var routeId = ObjectId.GenerateNewId();
            _mockRouteService
                .Setup(x => x.UpdateRouteAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<RouteForUpdateDTO>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(UpdateResult.Failed);

            var routesController = new RoutesController(_mockRouteService.Object);

            // Act
            var actual = await routesController.UpdateRouteAsync(routeId, new RouteForUpdateDTO());

            // Assert
            Assert.IsType<NotFoundResult>(actual);
        }

        [Fact]
        public async Task DeleteRouteAsync_WhenCalled_Returns204()
        {
            // Arrange
            _mockRouteService
                .Setup(x => x.DeleteRouteAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var routesController = new RoutesController(_mockRouteService.Object);

            // Act
            var result = await routesController.DeleteRouteAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteRouteAsync_RouteNotExists_Returns404()
        {
            // Arrange
            _mockRouteService
                .Setup(x => x.DeleteRouteAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var routesController = new RoutesController(_mockRouteService.Object);

            // Act
            var result = await routesController.DeleteRouteAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

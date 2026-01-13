using Airport.Models.DTOs;
using Airport.Presentation.Controllers;

namespace Airport.Presentation.Tests
{
    public class RoutesControllerTests
    {
        #region Fields
        private RoutesController _routesController;
        private Mock<IRouteService> _mockRouteService;
        private ILogger<RoutesController> _mockLogger;
        #endregion

        public RoutesControllerTests()
        {
            _mockLogger = Mock.Of<ILogger<RoutesController>>();
            _mockRouteService = new Mock<IRouteService>();
            _routesController = new RoutesController(_mockRouteService.Object);
        }

        [Fact]
        public void Created_NotNull() => Assert.NotNull(_routesController);

        [Fact]
        public async Task GetAllRoutesAsync_WhenCalled_ReturnsAllRoutes()
        {
            var routes = new RouteDTO[]
            {
                new RouteDTO(),
                new RouteDTO()
            };

            _mockRouteService
                .Setup(x => x.GetAllRoutesAsync(It.IsAny<CancellationToken>()))
                .Returns(routes.ToAsyncEnumerable());

            var result = await _routesController
                .GetAllRoutesAsync()
                .ToListAsync();

            var okResult = Assert.IsType<List<RouteDTO>>(result);
            Assert.Equal(routes, okResult);
        }

        [Fact]
        public async Task GetRouteByIdAsync_WhenCalled_Returns200WithRouteRequestedAsync()
        {
            var routeDto = new RouteDTO();
            routeDto.RouteId = ObjectId.GenerateNewId();

            _mockRouteService
                .Setup(x => x.GetRouteByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(routeDto);

            var result = await _routesController.GetRouteByIdAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal(routeDto.RouteId, (okResult.Value as RouteDTO)!.RouteId);
        }

        [Fact]
        public async Task GetRouteByIdAsync_RouteNotExists_Returns404()
        {
            _mockRouteService
                .Setup(x => x.GetRouteByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(RouteDTO?));

            var result = await _routesController.GetRouteByIdAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            var okResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, okResult.StatusCode);
        }

        [Fact]
        public async Task PostRouteAsync_WhenCalled_Returns201Async()
        {
            var routeForCreationDto = new RouteForCreationDTO();
            var id = ObjectId.GenerateNewId();
            var expected = _routesController.CreatedAtRoute(
                "RouteById",
                new { id },
                routeForCreationDto);

            var actual = await _routesController.PostRouteAsync(
                routeForCreationDto,
                It.IsAny<CancellationToken>());

            Assert.Equal(expected.StatusCode, (actual as CreatedAtRouteResult)!.StatusCode);
            Assert.Equal(expected.Value, (actual as CreatedAtRouteResult)!.Value);
        }

        [Fact]
        public async Task UpdateRouteAsync_ValidInput_Returns204()
        {

        }

        [Fact]
        public async Task UpdateRouteAsync_NullValue_Returns400()
        {

        }

        [Fact]
        public async Task DeleteRouteAsync_WhenCalled_Returns204()
        {
            _mockRouteService
                .Setup(x => x.DeleteRouteAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _routesController.DeleteRouteAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteRouteAsync_RouteNotExists_Returns404()
        {
            _mockRouteService
                .Setup(x => x.DeleteRouteAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _routesController.DeleteRouteAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>());

            Assert.IsType<NotFoundResult>(result);
        }
    }
}

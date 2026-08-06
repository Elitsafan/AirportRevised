namespace Airport.Domain.Tests.Creators
{
    public class RouteLogicCreatorTests
    {
        #region Fields
        private readonly Mock<IStationLogicProvider> _mockStationLogicProvider;
        private readonly Mock<IDirectionLogicProvider> _mockDirectionLogicProvider;
        private readonly ILogger<RouteLogic> _mockRouteLogicLogger;
        #endregion

        public RouteLogicCreatorTests()
        {
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
            _mockDirectionLogicProvider = new Mock<IDirectionLogicProvider>();
            _mockRouteLogicLogger = Mock.Of<ILogger<RouteLogic>>();
        }

        [Fact]
        public async Task CreateAsync_WhenCalled_ReturnsRouteLogicWithCorrectValues()
        {
            // Arrange
            var mockStationLogic = new Mock<IStationLogic>();
            mockStationLogic
                .SetupGet(s => s.StationId)
                .Returns(ObjectId.GenerateNewId());
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                RouteName = "TestRoute",
                Directions = new()
                {
                    new()
                    {
                        From = ObjectId.GenerateNewId(),
                        To = ObjectId.GenerateNewId() // This ensures the station is NOT filtered out
                    }
                }
            };

            _mockStationLogicProvider
                .Setup(x => x.GetByRouteIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { mockStationLogic.Object });

            // Act
            var routeLogicCreator = new RouteLogicCreator(
                route,
                It.IsAny<IEnumerable<ISectionLogic>>(),
                It.IsAny<IEnumerable<IStationLogic>>(),
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRouteLogicLogger);

            // Assert
            var routeLogic = Assert.IsType<RouteLogic>(await routeLogicCreator.CreateAsync());
            Assert.Equal(route.RouteName, routeLogic.RouteName);
            Assert.Equal(route.RouteId, routeLogic.RouteId);
        }

        [Fact]
        public async Task CreateAsync_WhenSectionsIsNotNull_ReturnsRouteLogicWithCorrectValues()
        {
            // Arrange
            var mockStationLogic = new Mock<IStationLogic>();
            var mockSectionLogic = new Mock<ISectionLogic>();
            mockSectionLogic
                .SetupGet(s => s.TrafficLights)
                .Returns(new HashSet<IStationLogic>()
                {
                    mockStationLogic.Object
                });
            mockStationLogic
                .SetupGet(s => s.StationId)
                .Returns(ObjectId.GenerateNewId());
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                RouteName = "TestRoute",
                Directions = new()
                {
                    new()
                    {
                        From = ObjectId.GenerateNewId(),
                        To = ObjectId.GenerateNewId() // This ensures the station is NOT filtered out
                    }
                }
            };

            _mockStationLogicProvider
                .Setup(x => x.GetByRouteIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { mockStationLogic.Object });

            // Act
            var routeLogicCreator = new RouteLogicCreator(
                route,
                new [] { mockSectionLogic.Object },
                It.IsAny<IEnumerable<IStationLogic>>(),
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRouteLogicLogger);

            // Assert
            var routeLogic = Assert.IsType<RouteLogic>(await routeLogicCreator.CreateAsync());
            Assert.Equal(route.RouteName, routeLogic.RouteName);
            Assert.Equal(route.RouteId, routeLogic.RouteId);
        }

        [Fact]
        public async Task CreateAsync_RouteNotFoundFromStationProvider_ThrowsLogicProvisionFailedException()
        {
            // Arrange
            var route = new Route { RouteId = ObjectId.GenerateNewId() };
            _mockStationLogicProvider
                .Setup(x => x.GetByRouteIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new LogicProvisionFailedException());

            // Act
            var routeLogicCreator = new RouteLogicCreator(
                route,
                It.IsAny<IEnumerable<ISectionLogic>>(),
                It.IsAny<IEnumerable<IStationLogic>>(),
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRouteLogicLogger);

            // Assert
            await Assert.ThrowsAsync<LogicProvisionFailedException>(() => routeLogicCreator.CreateAsync());
        }

        [Fact]
        public async Task CreateAsync_RouteNotFoundFromDirectionProvider_ThrowsEntityNotFoundException()
        {
            // Arrange
            var route = new Route { RouteId = ObjectId.GenerateNewId() };

            _mockDirectionLogicProvider
                .Setup(x => x.GetByRouteIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new LogicProvisionFailedException());

            // Act
            var routeLogicCreator = new RouteLogicCreator(
                route,
                It.IsAny<IEnumerable<ISectionLogic>>(),
                It.IsAny<IEnumerable<IStationLogic>>(),
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRouteLogicLogger);

            // Assert
            await Assert.ThrowsAsync<LogicProvisionFailedException>(() => routeLogicCreator.CreateAsync());
        }
    }
}

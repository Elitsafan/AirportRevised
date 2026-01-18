using Airport.Domain.Helpers;

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
            var mockSectionDetails = new Mock<IRouteSectionDetails>();
            var mockRouteSection = new Mock<IRouteSection>();
            mockRouteSection
                .SetupGet(rs => rs.AllTrafficLights)
                .Returns(new HashSet<IStationLogic>()
                {
                    mockStationLogic.Object
                });
            mockRouteSection
                .SetupGet(rs => rs.Destination)
                .Returns(new HashSet<IStationLogic>()
                {
                    mockStationLogic.Object
                });
            mockSectionDetails
                .SetupGet(s => s.RouteSection)
                .Returns(mockRouteSection.Object);
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
                .Setup(x => x.FindStationLogicsByRouteIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRouteSection.Object.Destination);

            // Act
            var routeLogicCreator = new RouteLogicCreator(
                route,
                _mockRouteLogicLogger,
                new List<IRouteSectionDetails>() { mockSectionDetails.Object },
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            // Assert
            var routeLogic = Assert.IsType<RouteLogic>(await routeLogicCreator.CreateAsync());
            Assert.Equal(route.RouteName, routeLogic.RouteName);
            Assert.Equal(route.RouteId, routeLogic.RouteId);
            Assert.Contains(mockRouteSection.Object.Destination.Single(), routeLogic.GetNextLeg());
        }

        [Fact]
        public async Task CreateAsync_WhenSectionsIsNull_ReturnsRouteLogicWithCorrectValues()
        {
            // Arrange           
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                RouteName = "routeName"
            };

            // Act
            var routeLogicCreator = new RouteLogicCreator(
                route,
                _mockRouteLogicLogger,
                null,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            // Assert
            var routeLogic = Assert.IsType<RouteLogic>(await routeLogicCreator.CreateAsync());
            Assert.Equal(route.RouteName, routeLogic.RouteName);
            Assert.Equal(route.RouteId, routeLogic.RouteId);
            Assert.Equal(Enumerable.Empty<IStationLogic>(), routeLogic.GetNextLeg());
        }

        [Fact]
        public async Task CreateAsync_RouteNotFoundFromStationProvider_ThrowsEntityNotFoundException()
        {
            // Arrange
            var route = new Route { RouteId = ObjectId.GenerateNewId() };
            _mockStationLogicProvider
                .Setup(x => x.FindStationLogicsByRouteIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityNotFoundException());

            // Act
            var routeLogicCreator = new RouteLogicCreator(
                route,
                _mockRouteLogicLogger,
                null,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            // Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(routeLogicCreator.CreateAsync);
            Assert.Equal("Route not found. Cannot create route logic.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_RouteNotFoundFromDirectionProvider_ThrowsEntityNotFoundException()
        {
            // Arrange
            var route = new Route { RouteId = ObjectId.GenerateNewId() };
            _mockDirectionLogicProvider
                .Setup(x => x.GetDirectionsByRouteIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityNotFoundException());

            // Act
            var routeLogicCreator = new RouteLogicCreator(
                route,
                _mockRouteLogicLogger,
                null,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            // Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(routeLogicCreator.CreateAsync);
            Assert.Equal("Route not found. Cannot create route logic.", ex.Message);
        }
    }
}

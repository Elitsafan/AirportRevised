namespace Airport.Domain.Tests.Logics
{
    public class RouteLogicTests
    {
        #region Fields
        private readonly ILogger<RouteLogic> _mockLogger;
        private Mock<IDirectionLogicProvider> _mockDirectionLogicProvider;
        private Mock<IStationLogicProvider> _mockStationLogicProvider;
        private IRouteLogic _routeLogic = null!;
        #endregion

        public RouteLogicTests()
        {
            _mockLogger = Mock.Of<ILogger<RouteLogic>>();
            _mockDirectionLogicProvider = new Mock<IDirectionLogicProvider>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
        }

        [Fact]
        public async Task EnterLegAsync_WhenCalled_ReturnsEnteredStation()
        {
            // Arrange
            var mockFlightLogic = new Mock<IFlightLogic>();
            var mockStations = Enumerable.Repeat(new Mock<IStationLogic>(), 2).ToArray();
            mockStations[0]
                .Setup(x => x.SetFlightAsync(
                    mockFlightLogic.Object,
                    It.IsAny<CancellationTokenSource>()))
                .ReturnsAsync(mockStations[0].Object);

            _routeLogic = new RouteLogic(
                new Route(),
                _mockLogger,
                Enumerable.Empty<IRouteSectionDetails>(),
                mockStations.Select(ms => ms.Object),
                Enumerable.Empty<IDirectionLogic>(),
                Enumerable.Empty<IStationLogic>());

            // Act
            var enteredStation = await _routeLogic.EnterLegAsync(
                mockFlightLogic.Object,
                _routeLogic.GetNextLeg());

            // Assert
            Assert.NotNull(enteredStation);
            mockStations[0].Verify(
                x => x.SetFlightAsync(
                    mockFlightLogic.Object,
                    It.IsAny<CancellationTokenSource>()),
                Times.Once());
        }

        [Fact]
        public async Task EnterLegAsync_WhenCalled_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockFlightLogic = new Mock<IFlightLogic>();
            var mockStations = new Mock<IStationLogic>[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>()
            };
            mockStations[0]
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.GenerateNewId());
            mockStations[1]
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.GenerateNewId());

            _routeLogic = new RouteLogic(
                new Route(),
                _mockLogger,
                Enumerable.Empty<IRouteSectionDetails>(),
                mockStations.Select(ms => ms.Object),
                Enumerable.Empty<IDirectionLogic>(),
                Enumerable.Empty<IStationLogic>());
            var mockLogger = Mock.Of<ILogger<IStationLogic>>();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _routeLogic.EnterLegAsync(
                mockFlightLogic.Object,
                _routeLogic
                    .GetNextLeg()
                    .Append(new Mock<IStationLogic>().Object)));
        }

        [Fact]
        public void GetNextLeg_WhenCalled_ReturnsFirstStation()
        {
            // Arrange
            var mockStations = new Mock<IStationLogic>[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>()
            };
            mockStations[0]
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.GenerateNewId());
            mockStations[1]
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.GenerateNewId());

            _routeLogic = new RouteLogic(
                new Route(),
                _mockLogger,
                Enumerable.Empty<IRouteSectionDetails>(),
                mockStations.Select(ms => ms.Object),
                Enumerable.Empty<IDirectionLogic>(),
                Enumerable.Empty<IStationLogic>());

            // Act
            var nextLeg = _routeLogic.GetNextLeg();

            // Assert
            Assert.Contains(nextLeg, item => item.StationId == mockStations[0].Object.StationId);
        }

        [Fact]
        public void GetNextLeg_WhenCalled_ReturnsNextStation()
        {
            // Arrange
            var route = new Route
            {
                Directions = new List<Direction>
                {
                    new()
                    {
                        From = ObjectId.Parse("000000000000000000000001"),
                        To = ObjectId.Parse("000000000000000000000002")
                    }
                }
            };
            var mockStationLogicLogger = new Mock<ILogger<IStationLogic>>();
            var mockStationLogic1 = new Mock<IStationLogic>();
            var mockStationLogic2 = new Mock<IStationLogic>();
            var mockStations = new Mock<IStationLogic>[]
            {
                mockStationLogic1,
                mockStationLogic2
            };
            var mockDirection1 = new Mock<IDirectionLogic>();
            var mockDirections = new Mock<IDirectionLogic>[] { mockDirection1 };

            mockStationLogic1
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.Parse("000000000000000000000001"));
            mockStationLogic2
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.Parse("000000000000000000000002"));
            mockDirection1
                .SetupGet(x => x.From)
                .Returns(ObjectId.Parse("000000000000000000000001"));
            mockDirection1
                .SetupGet(x => x.To)
                .Returns(ObjectId.Parse("000000000000000000000002"));

            _routeLogic = new RouteLogic(
               route,
                _mockLogger,
                Enumerable.Empty<IRouteSectionDetails>(),
                mockStations.Select(ms => ms.Object),
                mockDirections.Select(md => md.Object),
                Enumerable.Empty<IStationLogic>());

            // Act
            var nextLeg = _routeLogic.GetNextLeg(mockStationLogic1.Object);

            // Assert
            Assert.Contains(nextLeg, item => item.StationId == mockStations[1].Object.StationId);
        }
    }
}

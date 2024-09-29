namespace Airport.Domain.Tests.Logics
{
    public class RouteLogicTests
    {
        #region Fields
        private ILogger<RouteLogic> _mockLogger;
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
        public async Task RouteLogic_Created_NotNullAsync()
        {
            _mockStationLogicProvider
                .Setup(x => x.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IStationLogic[]
                {
                    new Mock<IStationLogic>().Object,
                });

            _routeLogic = await RouteLogic.CreateAsync(
                new Route(),
                It.IsAny<IEnumerable<IRouteSectionDetails>>(),
                _mockLogger,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            Assert.NotNull(_routeLogic);
        }

        [Fact]
        public async Task RouteLogic_RouteNotExist_ThrowsInvalidOperationExceptionAsync()
        {
            _mockStationLogicProvider
                .Setup(x => x.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Route not found."));
            await Assert.ThrowsAsync<InvalidOperationException>(() => RouteLogic.CreateAsync(
                new Route(),
                It.IsAny<IEnumerable<IRouteSectionDetails>>(),
                _mockLogger,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object));
        }

        [Fact]
        public async Task StartRunAsync_WhenCalled_ReturnsValueAsync()
        {
            _mockStationLogicProvider
                .Setup(x => x.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Repeat(Mock.Of<IStationLogic>(), 2));
            _routeLogic = await RouteLogic.CreateAsync(
                new Route(),
                It.IsAny<IEnumerable<IRouteSectionDetails>>(),
                _mockLogger,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            var releaser = await _routeLogic.StartRunAsync();
            releaser.Dispose();
        }

        [Fact]
        public async Task EnterLegAsync_WhenCalled_ReturnsEnteredStationAsync()
        {
            var mockFlightLogic = new Mock<IFlightLogic>();
            var mockStations = Enumerable.Repeat(new Mock<IStationLogic>(), 2).ToArray();
            _mockStationLogicProvider
                .Setup(x => x.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockStations.Select(ms => ms.Object));
            mockStations[0]
                .Setup(x => x.SetFlightAsync(mockFlightLogic.Object, It.IsAny<CancellationTokenSource>()))
                .ReturnsAsync(mockStations[0].Object);

            _routeLogic = await RouteLogic.CreateAsync(
                new Route(),
                It.IsAny<IEnumerable<IRouteSectionDetails>>(),
                _mockLogger,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            var enteredStation = await _routeLogic.EnterLegAsync(mockFlightLogic.Object, _routeLogic.GetNextLeg());

            Assert.NotNull(enteredStation);
        }

        [Fact]
        public async Task EnterLegAsync_WhenCalled_ThrowsInvalidOperationExceptionAsync()
        {
            var mockFlightLogic = new Mock<IFlightLogic>();
            var mockStations = new Mock<IStationLogic>[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>()
            };
            _mockStationLogicProvider
                .Setup(x => x.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockStations.Select(ms => ms.Object).ToArray());
            mockStations[0]
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.GenerateNewId());
            mockStations[1]
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.GenerateNewId());

            _routeLogic = await RouteLogic.CreateAsync(
                new Route(),
                It.IsAny<IEnumerable<IRouteSectionDetails>>(),
                _mockLogger,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);
            var mockLogger = Mock.Of<ILogger<IStationLogic>>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => _routeLogic.EnterLegAsync(
                mockFlightLogic.Object,
                _routeLogic
                    .GetNextLeg()
                    .Append(new Mock<IStationLogic>().Object)));
        }

        [Fact]
        public async Task GetNextLeg_WhenCalled_ReturnsFirstStationAsync()
        {
            var mockStationLogic1 = Mock.Of<IStationLogic>();

            _mockStationLogicProvider
                .Setup(x => x.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IStationLogic[]
                {
                        mockStationLogic1,
                });

            _routeLogic = await RouteLogic.CreateAsync(
                new Route(),
                It.IsAny<IEnumerable<IRouteSectionDetails>>(),
                _mockLogger,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            Assert.Contains(
                _routeLogic.GetNextLeg(),
                item => item == mockStationLogic1);
        }

        [Fact]
        public async Task GetNextLeg_WhenCalled_ReturnsNextStationAsync()
        {
            var mockStationLogicLogger = new Mock<ILogger<IStationLogic>>();
            var mockStationLogic1 = new Mock<IStationLogic>();
            var mockStationLogic2 = new Mock<IStationLogic>();
            var mockDirectionLogic = new Mock<IDirectionLogic>();

            mockStationLogic1
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.Parse("000000000000000000000001"));
            mockStationLogic2
                .SetupGet(x => x.StationId)
                .Returns(ObjectId.Parse("000000000000000000000002"));
            mockDirectionLogic
                .SetupGet(x => x.From)
                .Returns(ObjectId.Parse("000000000000000000000001"));
            mockDirectionLogic
                .SetupGet(x => x.To)
                .Returns(ObjectId.Parse("000000000000000000000002"));

            _mockStationLogicProvider
                .Setup(x => x.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IStationLogic[]
                {
                        mockStationLogic1.Object,
                        mockStationLogic2.Object,
                });
            _mockDirectionLogicProvider
                .Setup(x => x.GetDirectionsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IDirectionLogic[] { mockDirectionLogic.Object });

            _routeLogic = await RouteLogic.CreateAsync(
                new Route(),
                It.IsAny<IEnumerable<IRouteSectionDetails>>(),
                _mockLogger,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            Assert.Contains(
                _routeLogic.GetNextLeg(mockStationLogic1.Object),
                item => item == mockStationLogic2.Object);
        }
    }
}

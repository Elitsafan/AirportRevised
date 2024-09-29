namespace Airport.Domain.Tests.Creators
{
    public class RouteLogicCreatorTests
    {
        #region Fields
        private Mock<IRepositoryManager> _mockRepositoryManager;
        private Mock<IStationLogicProvider> _mockStationLogicProvider;
        private Mock<IDirectionLogicProvider> _mockDirectionLogicProvider;
        private ILogger<RouteLogic> _mockRouteLogicLogger;
        #endregion

        public RouteLogicCreatorTests()
        {
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
            _mockDirectionLogicProvider = new Mock<IDirectionLogicProvider>();
            _mockRouteLogicLogger = Mock.Of<ILogger<RouteLogic>>();

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(Mock.Of<IRouteRepository>());
        }

        [Fact]
        public async Task CreateRouteLogic_ReturnsNotNullAsync()
        {
            _mockStationLogicProvider
                .Setup(x => x.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IStationLogic[] { Mock.Of<IStationLogic>() });

            var creator = new RouteLogicCreator(
                new Route(),
                It.IsAny<IEnumerable<IRouteSectionDetails>>(),
                _mockRouteLogicLogger,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);

            var routeLogic = await creator.CreateAsync();
            Assert.NotNull(routeLogic);
        }
    }
}

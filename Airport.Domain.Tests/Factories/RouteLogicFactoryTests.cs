namespace Airport.Domain.Tests.Factories
{
    public class RouteLogicFactoryTests
    {
        #region Fields
        private readonly ILogger<RouteLogic> _mockLogger;
        private readonly Mock<IDirectionLogicProvider> _mockDirectionLogicProvider;
        private readonly Mock<IStationLogicProvider> _mockStationLogicProvider;
        private readonly IRouteLogicFactory _routeLogicFactory;
        #endregion

        public RouteLogicFactoryTests()
        {
            _mockLogger = Mock.Of<ILogger<RouteLogic>>();
            _mockDirectionLogicProvider = new Mock<IDirectionLogicProvider>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();

            _routeLogicFactory = new RouteLogicFactory(
                _mockLogger,
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object);
        }

        [Fact]
        public void GetCreator_WhenCalled_ReturnsRouteLogicCreator()
        {
            // Arrange
            var route = new Route();
            var sections = new List<IRouteSectionDetails> { new Mock<IRouteSectionDetails>().Object };

            // Act
            var result = _routeLogicFactory.GetCreator(route, sections);

            // Assert
            Assert.IsType<RouteLogicCreator>(result);
        }

        [Fact]
        public void GetCreator_RouteIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _routeLogicFactory.GetCreator(
                null!,
                Enumerable.Empty<IRouteSectionDetails>()));

            Assert.Equal("route", ex.ParamName);
        }
    }
}

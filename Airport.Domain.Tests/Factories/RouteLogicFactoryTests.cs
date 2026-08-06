namespace Airport.Domain.Tests.Factories
{
    public class RouteLogicFactoryTests
    {
        #region Fields
        private readonly ILogger<RouteLogic> _mockLogger;
        private readonly Mock<IDirectionLogicProvider> _mockDirectionLogicProvider;
        private readonly Mock<IStationLogicProvider> _mockStationLogicProvider;
        private readonly IRouteLogicFactory _sut;
        #endregion

        public RouteLogicFactoryTests()
        {
            _mockLogger = Mock.Of<ILogger<RouteLogic>>();
            _mockDirectionLogicProvider = new Mock<IDirectionLogicProvider>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();

            _sut = new RouteLogicFactory(
                _mockDirectionLogicProvider.Object,
                _mockStationLogicProvider.Object,
                _mockLogger);
        }

        [Fact]
        public void GetCreator_WhenCalled_ReturnsRouteLogicCreator()
        {
            // Arrange
            var route = new Route();
            var sections = new List<ISectionLogic> { Mock.Of<ISectionLogic>() };
            var standaloneTLs = new List<IStationLogic> { Mock.Of<IStationLogic>() };

            // Act
            var result = _sut.GetCreator(route, sections, standaloneTLs);

            // Assert
            Assert.IsType<RouteLogicCreator>(result);
        }

        [Fact]
        public void GetCreator_RouteIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _sut.GetCreator(
                null!,
                Enumerable.Empty<ISectionLogic>(),
                Enumerable.Empty<IStationLogic>()));

            Assert.Equal("route", ex.ParamName);
        }
    }
}

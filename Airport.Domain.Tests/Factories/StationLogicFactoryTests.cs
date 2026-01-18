namespace Airport.Domain.Tests.Factories
{
    public class StationLogicFactoryTests
    {
        #region Fields
        private IStationLogicFactory _stationLogicFactory = null!;
        private readonly ILogger<StationLogic> _logger;
        #endregion

        public StationLogicFactoryTests() => _logger = Mock.Of<ILogger<StationLogic>>();

        [Fact]
        public void GetCreator_WhenCalled_ReturnsStationLogicCreator()
        {
            // Arrange
            var station = new Station();
            _stationLogicFactory = new StationLogicFactory(_logger);

            // Act
            var result = _stationLogicFactory.GetCreator(station);

            // Assert
            Assert.IsType<StationLogicCreator>(result);
        }

        [Fact]
        public void GetCreator_StationIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            _stationLogicFactory = new StationLogicFactory(_logger);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(() => _stationLogicFactory.GetCreator(null!));
            Assert.Equal("station", ex.ParamName);
        }
    }
}

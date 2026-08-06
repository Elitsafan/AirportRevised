namespace Airport.Domain.Tests.Factories
{
    public class StationLogicFactoryTests
    {
        #region Fields
        private IStationLogicFactory _sut = null!;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly ILogger<StationLogic> _logger;
        #endregion

        public StationLogicFactoryTests()
        {
            _mockDomainEvents = new Mock<IDomainEvents>();
            _logger = Mock.Of<ILogger<StationLogic>>();
        }

        [Fact]
        public void GetCreator_WhenCalled_ReturnsStationLogicCreator()
        {
            // Arrange
            var station = new Station();
            _sut = new StationLogicFactory(_mockDomainEvents.Object, _logger);

            // Act
            var result = _sut.GetCreator(station);

            // Assert
            Assert.IsType<StationLogicCreator>(result);
        }

        [Fact]
        public void GetCreator_StationIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            _sut = new StationLogicFactory(_mockDomainEvents.Object, _logger);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => _sut.GetCreator(null!));
            Assert.Equal("station", ex.ParamName);
        }
    }
}

namespace Airport.Domain.Tests.Creators
{
    public class StationLogicCreatorTests
    {
        [Fact]
        public void Create_WhenCalled_ReturnsStationLogicWithCorrectValues()
        {
            // Arrange
            var mockDomainEvents = new Mock<IDomainEvents>();
            var station = new Station
            {
                StationId = ObjectId.GenerateNewId(),
                EstimatedWaitingTime = TimeSpan.FromSeconds(555),
            };
            var mockLogger = Mock.Of<ILogger<StationLogic>>();

            var creator = new StationLogicCreator(station, mockDomainEvents.Object, mockLogger);

            // Act
            var result = creator.Create();

            // Assert
            var stationLogic = Assert.IsType<StationLogic>(result);

            Assert.Equal(station.StationId, stationLogic.StationId);
            Assert.Equal(station.EstimatedWaitingTime, stationLogic.EstimatedWaitingTime);
            Assert.Null(stationLogic.CurrentFlightType);
            Assert.Null(stationLogic.CurrentFlightId);
        }
    }
}

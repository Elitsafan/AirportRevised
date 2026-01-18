namespace Airport.Domain.Tests.Creators
{
    public class StationLogicCreatorTests
    {
        [Fact]
        public void Create_WhenCalled_ReturnsStationLogicWithCorrectValues()
        {
            // Arrange
            var station = new Station
            {
                StationId = ObjectId.GenerateNewId(),
                EstimatedWaitingTime = TimeSpan.FromSeconds(555),
            };
            var mockLogger = Mock.Of<ILogger<StationLogic>>();
            var result = new StationLogicCreator(station, mockLogger);

            // Act
            var creator = result.Create();

            // Assert
            var stationLogic = Assert.IsType<StationLogic>(creator);

            Assert.Equal(station.StationId, stationLogic.StationId);
            Assert.Equal(station.EstimatedWaitingTime, stationLogic.EstimatedWaitingTime);
            Assert.Null(stationLogic.CurrentFlightType);
            Assert.Null(stationLogic.CurrentFlightId);
        }
    }
}

namespace Airport.Domain.Tests.Creators
{
    public class StationLogicCreatorTests
    {
        [Fact]
        public void CreateStationLogic_ReturnsNotNull()
        {
            var station = new Station();
            IStationLogicCreator creator = new StationLogicCreator(station, Mock.Of<ILogger<IStationLogic>>());
            var stationLogic = creator.Create();

            Assert.NotNull(stationLogic);
        }
    }
}

namespace Airport.Domain.Tests.Creators
{
    public class SyncerLogicCreatorTests
    {
        [Fact]
        public void Create_WhenCalled_ReturnsSyncerLogicWithCorrectData()
        {
            // Arrange
            var syncer = new Syncer
            {
                SyncerId = ObjectId.GenerateNewId(),
                SectionCriticalOccupations = new()
                {
                    new SectionCriticalOccupation
                    {
                        RouteId = ObjectId.GenerateNewId(),
                        Value = 10
                    }
                }
            };

            var creator = new SyncerLogicCreator(syncer, Mock.Of<ILogger<SyncerLogic>>());

            // Act
            var result = creator.Create();

            // Assert
            Assert.IsType<SyncerLogic>(result);
            Assert.Equal(syncer.SyncerId, result.SyncerId);
        }
    }
}

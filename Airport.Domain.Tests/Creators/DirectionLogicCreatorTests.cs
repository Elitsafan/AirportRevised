namespace Airport.Domain.Tests.Creators
{
    public class DirectionLogicCreatorTests
    {
        [Fact]
        public void Create_WhenCalled_ReturnsDirectionLogicWithCorrectData()
        {
            // Arrange
            var direction = new Direction
            {
                From = ObjectId.GenerateNewId(),
                To = ObjectId.GenerateNewId()
            };
            var creator = new DirectionLogicCreator(direction);

            // Act
            var result = creator.Create();

            // Assert
            Assert.IsType<DirectionLogic>(result);
            Assert.Equal(direction.From, result.From);
            Assert.Equal(direction.To, result.To);
        }
    }
}

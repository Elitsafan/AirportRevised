namespace Airport.Domain.Tests.Creators
{
    public class DirectionLogicCreatorTests
    {
        [Fact]
        public void CreateDirectionLogic_ReturnsNotNull()
        {
            var direction = new Direction();
            IDirectionLogicCreator creator = new DirectionLogicCreator(direction);
            var directionLogic = creator.Create();

            Assert.NotNull(directionLogic);
        }
    }
}

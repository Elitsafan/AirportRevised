namespace Airport.Domain.Tests.Factories
{
    public class DirectionLogicFactoryTests
    {
        private readonly IDirectionLogicFactory _directionLogicFactory;

        public DirectionLogicFactoryTests() => _directionLogicFactory = new DirectionLogicFactory();

        [Fact]
        public void GetCreator_WhenCalled_ReturnsDirectionLogicCreator()
        {
            // Arrange
            var direction = new Direction();

            // Act
            var result = _directionLogicFactory.GetCreator(direction);

            // Assert
            Assert.IsType<DirectionLogicCreator>(result);
        }

        [Fact]
        public void GetCreator_DirectionIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _directionLogicFactory.GetCreator(null!));

            Assert.Equal("direction", ex.ParamName);
        }
    }
}

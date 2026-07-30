namespace Airport.Domain.Tests.Factories
{
    public class SyncerLogicFactoryTests
    {
        private readonly ISyncerLogicFactory _sut;

        public SyncerLogicFactoryTests() => _sut = new SyncerLogicFactory(Mock.Of<ILogger<SyncerLogic>>());

        [Fact]
        public void GetCreator_WhenCalled_ReturnsSyncerLogicCreator()
        {
            // Arrange
            var syncer = new Syncer();

            // Act
            var result = _sut.GetCreator(syncer);

            // Assert
            Assert.IsType<SyncerLogicCreator>(result);
        }

        [Fact]
        public void GetCreator_SyncerIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _sut.GetCreator(null!));

            Assert.Equal("syncer", ex.ParamName);
        }
    }
}

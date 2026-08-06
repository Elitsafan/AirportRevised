namespace Airport.Domain.Tests.Factories
{
    public class SectionLogicFactoryTests
    {
        #region Fields
        private readonly Mock<IStationLogicProvider> _mockStationProvider;
        private readonly Mock<ISyncerLogicProvider> _mockSyncerProvider;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly ILogger<SectionLogic> _mockLogger;
        #endregion

        public SectionLogicFactoryTests()
        {
            _mockStationProvider = new Mock<IStationLogicProvider>();
            _mockSyncerProvider = new Mock<ISyncerLogicProvider>();
            _mockDomainEvents = new Mock<IDomainEvents>();
            _mockLogger = Mock.Of<ILogger<SectionLogic>>();
        }

        [Fact]
        public void GetCreator_WhenCalled_ReturnsSectionLogicCreator()
        {
            // Arrange
            var sectionLogicFactory = new SectionLogicFactory(
                _mockStationProvider.Object,
                _mockSyncerProvider.Object,
                _mockDomainEvents.Object,
                _mockLogger);

            var section = new Section
            {
                RouteId = ObjectId.GenerateNewId(),
                Origin = new List<ObjectId>
                {
                    ObjectId.GenerateNewId()
                },
                SectionOnly = new List<ObjectId>
                {
                    ObjectId.GenerateNewId()
                },
                Destination = new List<ObjectId>
                {
                    ObjectId.GenerateNewId()
                }
            };

            // Act & Assert
            var result = Assert.IsType<SectionLogicCreator>(sectionLogicFactory.GetCreator(section));
            Assert.NotNull(result);
        }
    }
}

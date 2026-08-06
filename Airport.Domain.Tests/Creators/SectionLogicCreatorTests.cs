namespace Airport.Domain.Tests.Creators
{
    public class SectionLogicCreatorTests
    {
        [Fact]
        public async Task CreateAsync_WhenCalled_ReturnsSectionLogicWithCorrectData()
        {
            // Arrange
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
            var mockStations = new[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>()
            };
            mockStations[0]
                .SetupGet(x => x.StationId)
                .Returns(section.Origin[0]);
            mockStations[1]
                .SetupGet(x => x.StationId)
                .Returns(section.SectionOnly[0]);
            mockStations[2]
                .SetupGet(x => x.StationId)
                .Returns(section.Destination[0]);

            var mockStationProvider = new Mock<IStationLogicProvider>();

            mockStationProvider
                .Setup(x => x.GetByRouteIdAsync(section.RouteId, default))
                .ReturnsAsync(mockStations.Select(s => s.Object).ToList());

            var mockRouteSyncerProvider = new Mock<ISyncerLogicProvider>();
            var mockLogger = Mock.Of<ILogger<SectionLogic>>();
            var mockDomainEvents = new Mock<IDomainEvents>();

            var creator = new SectionLogicCreator(
                section,
                mockStationProvider.Object,
                mockRouteSyncerProvider.Object,
                mockDomainEvents.Object,
                mockLogger);

            // Act
            var sectionLogic = await creator.CreateAsync();

            // Assert
            var result = Assert.IsType<SectionLogic>(sectionLogic);
            Assert.Equal(section.RouteId, result.RouteId);
            Assert.Contains(section.Origin, id => id == mockStations[0].Object.StationId);
            Assert.Contains(section.SectionOnly, id => id == mockStations[1].Object.StationId);
            Assert.Contains(section.Destination, id => id == mockStations[2].Object.StationId);
        }
    }
}

namespace Airport.Domain.Tests.Logics
{
    public class SectionLogicTests
    {
        [Fact]
        public async Task EnterSectionAsync_WhenCalled_EnteredSection()
        {
            // Arrange
            var flightId = ObjectId.GenerateNewId();

            var section = new Section
            {
                SectionId = ObjectId.GenerateNewId(),
                RouteId = ObjectId.GenerateNewId(),
                SyncerId = ObjectId.GenerateNewId(),
                Origin = new()
                {
                    ObjectId.GenerateNewId(),
                    ObjectId.GenerateNewId(),
                },
                SectionOnly = new()
                {
                    ObjectId.GenerateNewId(),
                    ObjectId.GenerateNewId(),
                },
                Destination = new()
                {
                    ObjectId.GenerateNewId(),
                    ObjectId.GenerateNewId(),
                },
            };

            var mockSyncerLogic = new Mock<ISyncerLogic>();
            var mockDomainEvents = new Mock<IDomainEvents>();
            var mockOrigin = new[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
            };
            var mockSectionOnly = new[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
            };
            var mockDestination = new[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
            };

            mockOrigin[0].SetupGet(x => x.StationId).Returns(section.Origin[0]);
            mockOrigin[1].SetupGet(x => x.StationId).Returns(section.Origin[1]);

            mockSectionOnly[0].SetupGet(x => x.StationId).Returns(section.SectionOnly[0]);
            mockSectionOnly[1].SetupGet(x => x.StationId).Returns(section.SectionOnly[1]);

            mockDestination[0].SetupGet(x => x.StationId).Returns(section.Destination[0]);
            mockDestination[1].SetupGet(x => x.StationId).Returns(section.Destination[1]);

            mockSyncerLogic
                .Setup(x => x.EnterSectionAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AsyncSemaphore.Releaser());

            var cts = new CancellationTokenSource();

            var sut = new SectionLogic(
                section,
                mockSyncerLogic.Object,
                mockDomainEvents.Object,
                mockOrigin.Select(x => x.Object),
                mockSectionOnly.Select(x => x.Object),
                mockDestination.Select(x => x.Object),
                Mock.Of<ILogger<SectionLogic>>());

            // Act
            await sut.EnterSectionAsync(mockOrigin[0].Object, flightId, cts, default);

            // Assert
            mockSyncerLogic.Verify(
                x => x.EnterSectionAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void SectionLogicCreated_ReturnCorrectPropertiesValues()
        {
            // Arrange
            var section = new Section
            {
                SectionId = ObjectId.GenerateNewId(),
                RouteId = ObjectId.GenerateNewId(),
                SyncerId = ObjectId.GenerateNewId(),
                Origin = new()
                {
                    ObjectId.GenerateNewId(),
                    ObjectId.GenerateNewId(),
                },
                SectionOnly = new()
                {
                    ObjectId.GenerateNewId(),
                    ObjectId.GenerateNewId(),
                },
                Destination = new()
                {
                    ObjectId.GenerateNewId(),
                    ObjectId.GenerateNewId(),
                },
            };

            var mockSyncerLogic = new Mock<ISyncerLogic>();
            var mockDomainEvents = new Mock<IDomainEvents>();
            var mockOrigin = new[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
            };
            var mockSectionOnly = new[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
            };
            var mockDestination = new[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
            };

            mockOrigin[0].SetupGet(x => x.StationId).Returns(section.Origin[0]);
            mockOrigin[1].SetupGet(x => x.StationId).Returns(section.Origin[1]);

            mockSectionOnly[0].SetupGet(x => x.StationId).Returns(section.SectionOnly[0]);
            mockSectionOnly[1].SetupGet(x => x.StationId).Returns(section.SectionOnly[1]);

            mockDestination[0].SetupGet(x => x.StationId).Returns(section.Destination[0]);
            mockDestination[1].SetupGet(x => x.StationId).Returns(section.Destination[1]);

            var cts = new CancellationTokenSource();

            // Act
            var sut = new SectionLogic(
                section,
                mockSyncerLogic.Object,
                mockDomainEvents.Object,
                mockOrigin.Select(x => x.Object),
                mockSectionOnly.Select(x => x.Object),
                mockDestination.Select(x => x.Object),
                Mock.Of<ILogger<SectionLogic>>());

            // Assert
            Assert.Equal(section.SectionId, sut.SectionId);
            Assert.Equal(section.RouteId, sut.RouteId);
            Assert.Equal(
                section.Origin.OrderBy(id => id),
                sut.Origin.Select(s => s.StationId).OrderBy(id => id));
            Assert.Equal(
                section.SectionOnly.OrderBy(id => id),
                sut.SectionOnly.Select(s => s.StationId).OrderBy(id => id));
            Assert.Equal(
                section.Destination.OrderBy(id => id),
                sut.Destination.Select(s => s.StationId).OrderBy(id => id));
        }
    }
}

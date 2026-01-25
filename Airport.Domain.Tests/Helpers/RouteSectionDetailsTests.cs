using Airport.Domain.Helpers;

namespace Airport.Domain.Tests.Helpers
{
    public class RouteSectionDetailsTests
    {
        #region Fields
        private readonly Mock<IRouteSection> _mockRouteSection;
        private readonly Mock<ISectionSynchronizerDetails> _mockSectionSynchronizerDetails;
        #endregion

        public RouteSectionDetailsTests()
        {
            _mockRouteSection = new Mock<IRouteSection>();
            _mockSectionSynchronizerDetails = new Mock<ISectionSynchronizerDetails>();
        }

        [Fact]
        public void Created_ReturnsCorrectValues()
        {
            // Arrange
            var destination = new HashSet<IStationLogic> { new Mock<IStationLogic>().Object };
            _mockRouteSection
                .SetupGet(x => x.Destination)
                .Returns(destination);

            // Act
            IRouteSectionDetails rsd = new RouteSectionDetails(
                _mockRouteSection.Object,
                _mockSectionSynchronizerDetails.Object);

            // Assert
            Assert.Equal(_mockRouteSection.Object, rsd.RouteSection);
        }

        [Fact]
        public async Task EnterSectionAsync_StationDoNotBelongToSource_ThrowsArgumentException()
        {
            // Arrange
            var source = new HashSet<IStationLogic> { new Mock<IStationLogic>().Object };
            var destination = new HashSet<IStationLogic> { new Mock<IStationLogic>().Object };
            var mockStationLogic = new Mock<IStationLogic>();

            _mockRouteSection
                .SetupGet(x => x.Destination)
                .Returns(destination);
            _mockRouteSection
                .SetupGet(x => x.Source)
                .Returns(source);

            // Act
            IRouteSectionDetails rsd = new RouteSectionDetails(
                _mockRouteSection.Object,
                _mockSectionSynchronizerDetails.Object);

            // Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => rsd.EnterSectionAsync(
                mockStationLogic.Object,
                ObjectId.GenerateNewId(),
                null,
                default));
            Assert.Equal("station", ex.ParamName);
        }

        [Fact]
        public async Task EnterSectionAsync_WhenCalled_CallsSynchronizerAndAddsToTrace()
        {
            // Arrange
            var mockStationLogic = new Mock<IStationLogic>();
            var flightId = ObjectId.GenerateNewId();
            var routeId = ObjectId.GenerateNewId();

            _mockRouteSection
                .SetupGet(x => x.Source)
                .Returns(new HashSet<IStationLogic>() { mockStationLogic.Object });
            _mockRouteSection
                .SetupGet(x => x.Destination)
                .Returns(new HashSet<IStationLogic>());
            _mockRouteSection
                .SetupGet(x => x.RouteId)
                .Returns(routeId);

            // Act
            IRouteSectionDetails rsd = new RouteSectionDetails(
                _mockRouteSection.Object,
                _mockSectionSynchronizerDetails.Object);
            await rsd.EnterSectionAsync(
                mockStationLogic.Object,
                flightId,
                null);

            // Assert
            _mockSectionSynchronizerDetails.Verify(
                s => s.EnterSectionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockSectionSynchronizerDetails.Verify(
                s => s.GetSourceRightOfWayAsync(routeId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Constructor_SubscribesToDestinationStationEvents()
        {
            // Arrange
            var mockDestStation = new Mock<IStationLogic>();
            var destination = new HashSet<IStationLogic> { mockDestStation.Object };
            _mockRouteSection
                .SetupGet(x => x.Destination)
                .Returns(destination);

            // Act
            var rsd = new RouteSectionDetails(
                _mockRouteSection.Object,
                _mockSectionSynchronizerDetails.Object);

            // Assert
            // Check if the += subscription happened (This requires the event to be mockable)
            mockDestStation.VerifyAdd(
                s => s.StationClearedAsync += It.IsAny<AsyncEventHandler<IStationClearedEventArgs>>(), Times.Once);
        }
    }
}

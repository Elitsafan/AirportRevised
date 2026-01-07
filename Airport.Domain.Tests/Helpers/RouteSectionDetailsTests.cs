using Airport.Domain.Helpers;

namespace Airport.Domain.Tests.Helpers
{
    public class RouteSectionDetailsTests
    {
        private readonly Mock<IRouteSection> _mockRouteSection;
        private readonly Mock<ISectionSynchronizerDetails> _mockSectionSynchronizerDetails;

        public RouteSectionDetailsTests()
        {
            _mockRouteSection = new Mock<IRouteSection>();
            _mockSectionSynchronizerDetails = new Mock<ISectionSynchronizerDetails>();
        }

        [Fact]
        public void Created_NotNull()
        {
            var source = new IStationLogic[] { new Mock<IStationLogic>().Object };
            var destination = new IStationLogic[] { new Mock<IStationLogic>().Object };
            var stations = new IStationLogic[] { new Mock<IStationLogic>().Object };

            _mockRouteSection
                .SetupGet(x => x.Destination)
                .Returns(destination.ToHashSet());

            IRouteSectionDetails rsd = new RouteSectionDetails(
                _mockRouteSection.Object,
                _mockSectionSynchronizerDetails.Object);

            Assert.NotNull(rsd);
        }

        [Fact]
        public async Task EnterSectionAsync_StationDoNotBelongToSource_ThrowsArgumentExceptionAsync()
        {
            var source = new IStationLogic[] { new Mock<IStationLogic>().Object };
            var destination = new IStationLogic[] { new Mock<IStationLogic>().Object };
            var stations = new IStationLogic[] { new Mock<IStationLogic>().Object };

            var mockStationLogic = new Mock<IStationLogic>();

            _mockRouteSection
                .SetupGet(x => x.Destination)
                .Returns(destination.ToHashSet());
            _mockRouteSection
                .SetupGet(x => x.Source)
                .Returns(source.ToHashSet());

            IRouteSectionDetails rsd = new RouteSectionDetails(
                _mockRouteSection.Object,
                _mockSectionSynchronizerDetails.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => rsd.EnterSectionAsync(
                mockStationLogic.Object,
                It.IsAny<ObjectId>(),
                null,
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task EnterSectionAsync_WhenCalled_EntersSectionAsync()
        {
            var mockStationLogic = new Mock<IStationLogic>();
            var source = new IStationLogic[] { mockStationLogic.Object };
            var destination = new IStationLogic[] { new Mock<IStationLogic>().Object };
            var stations = new IStationLogic[] { new Mock<IStationLogic>().Object };

            _mockRouteSection
                .SetupGet(x => x.Destination)
                .Returns(destination.ToHashSet());
            _mockRouteSection
                .SetupGet(x => x.Source)
                .Returns(source.ToHashSet());

            IRouteSectionDetails rsd = new RouteSectionDetails(
                _mockRouteSection.Object,
                _mockSectionSynchronizerDetails.Object);

            var task = rsd.EnterSectionAsync(
                mockStationLogic.Object,
                It.IsAny<ObjectId>(),
                null,
                It.IsAny<CancellationToken>());
            await task;

            Assert.True(task.IsCompletedSuccessfully);
        }
    }
}

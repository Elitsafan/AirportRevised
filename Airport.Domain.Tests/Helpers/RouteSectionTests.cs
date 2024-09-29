using Airport.Domain.Helpers;

namespace Airport.Domain.Tests.Helpers
{
    public class RouteSectionTests
    {
        [Fact]
        public void Created_NotNull()
        {
            var source = new IStationLogic[] { new Mock<IStationLogic>().Object };
            var destination = new IStationLogic[] { new Mock<IStationLogic>().Object };
            var stations = new IStationLogic[] { new Mock<IStationLogic>().Object };

            Assert.NotNull(
                new RouteSection(
                    source,
                    destination,
                    stations,
                    It.IsAny<ObjectId>()));
        }

        [Fact]
        public void AddToSource_WhenCalled_StationAddedToSourceAndAllStationsCountUpdated()
        {
            var source = new IStationLogic[] { new Mock<IStationLogic>().Object };
            var destination = new IStationLogic[] { new Mock<IStationLogic>().Object };
            var stations = new IStationLogic[] { new Mock<IStationLogic>().Object };

            var stationLogic = new Mock<IStationLogic>().Object;
            var routeSection = new RouteSection(
                source,
                destination,
                stations,
                It.IsAny<ObjectId>());

            var oldStationsCount = routeSection.AllStationsCount;
            routeSection.AddToSource(stationLogic);

            Assert.True(routeSection.Source.Contains(stationLogic));
            Assert.True(routeSection.AllStationsCount == oldStationsCount + 1);
        }

        [Theory]
        [MemberData(nameof(GetNullCtorParameters))]
        public void CreatedWithNullParameters_ThrowsArgumentNullException(
            IEnumerable<IStationLogic> source,
            IEnumerable<IStationLogic> destination,
            IEnumerable<IStationLogic> stations) => Assert.Throws<ArgumentNullException>(
                () => new RouteSection(
                    source,
                    destination,
                    stations,
                    It.IsAny<ObjectId>()));

        [Theory]
        [MemberData(nameof(GetEmptyCtorParameters))]
        public void CreatedWithEmptyCollections_ThrowsArgumentException(
            IEnumerable<IStationLogic> source,
            IEnumerable<IStationLogic> destination,
            IEnumerable<IStationLogic> stations) => Assert.Throws<ArgumentException>(
                () => new RouteSection(
                    source,
                    destination,
                    stations,
                    It.IsAny<ObjectId>()));

        public static IEnumerable<object[]> GetEmptyCtorParameters()
        {
            yield return new IStationLogic[3][]
            {
                Array.Empty<IStationLogic>(),
                new IStationLogic[] { new Mock<IStationLogic>().Object },
                new IStationLogic[] { new Mock<IStationLogic>().Object }
            };
            yield return new IStationLogic[3][]
            {
                new IStationLogic[] { new Mock<IStationLogic>().Object },
                Array.Empty<IStationLogic>(),
                new IStationLogic[] { new Mock<IStationLogic>().Object }
            };
            yield return new IStationLogic[3][]
            {
                new IStationLogic[] { new Mock<IStationLogic>().Object },
                new IStationLogic[] { new Mock<IStationLogic>().Object },
                Array.Empty < IStationLogic >()
            };
        }

        public static IEnumerable<object[]> GetNullCtorParameters()
        {
            yield return new IStationLogic[3][]
            {
                null!,
                new IStationLogic[] { new Mock<IStationLogic>().Object },
                new IStationLogic[] { new Mock<IStationLogic>().Object }
            };
            yield return new IStationLogic[3][]
            {
                new IStationLogic[] { new Mock<IStationLogic>().Object },
                null!,
                new IStationLogic[] { new Mock<IStationLogic>().Object }
            };
            yield return new IStationLogic[3][]
            {
                new IStationLogic[] { new Mock<IStationLogic>().Object },
                new IStationLogic[] { new Mock<IStationLogic>().Object },
                null!
            };
        }
    }
}

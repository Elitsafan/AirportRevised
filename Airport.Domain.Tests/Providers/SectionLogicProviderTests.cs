using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Tests.Providers
{
    public class SectionLogicProviderTests
    {
        #region Fields
        private SectionLogicProvider? _sut;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<ISectionRepository> _mockSectionRepository;
        private readonly MemoryCache _cache;
        private readonly ILogger<SectionLogicProvider> _mockLogger;
        private readonly Mock<ISectionLogicFactory> _mockSectionLogicFactory;
        private readonly Mock<ISectionLogicCreator> _mockSectionLogicCreator;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        #endregion

        public SectionLogicProviderTests()
        {
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockSectionRepository = new Mock<ISectionRepository>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = Mock.Of<ILogger<SectionLogicProvider>>();
            _mockSectionLogicFactory = new Mock<ISectionLogicFactory>();
            _mockSectionLogicCreator = new Mock<ISectionLogicCreator>();
            _mockDomainEvents = new Mock<IDomainEvents>();

            _mockRepositoryManager
                .SetupGet(x => x.SectionRepository)
                .Returns(_mockSectionRepository.Object);
            _mockSectionLogicFactory
                .Setup(x => x.GetCreator(It.IsAny<Section>()))
                .Returns(_mockSectionLogicCreator.Object);
        }

        [Fact]
        public async Task GetAllAsync_WhenCalled_ReturnsCorrectValues()
        {
            var mockSectionLogics = new[]
            {
                new Mock<ISectionLogic>(),
            };
            var mockStationLogics = new[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
            };
            var routeId = ObjectId.GenerateNewId();
            var sections = new List<Section>
            {
                new Section
                {
                    RouteId = routeId,
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
                }
            };

            mockSectionLogics[0]
                .SetupGet(x => x.Origin)
                .Returns(new List<IStationLogic> { mockStationLogics[0].Object });
            mockSectionLogics[0]
                .SetupGet(x => x.SectionOnly)
                .Returns(new List<IStationLogic> { mockStationLogics[1].Object });
            mockSectionLogics[0]
                .SetupGet(x => x.Destination)
                .Returns(new List<IStationLogic> { mockStationLogics[2].Object });
            mockStationLogics[0]
                .SetupGet(x => x.StationId)
                .Returns(sections[0].Origin[0]);
            mockStationLogics[1]
                .SetupGet(x => x.StationId)
                .Returns(sections[0].SectionOnly[0]);
            mockStationLogics[2]
                .SetupGet(x => x.StationId)
                .Returns(sections[0].Destination[0]);
            _mockSectionLogicCreator
                .SetupSequence(x => x.CreateAsync(default))
                .ReturnsAsync(mockSectionLogics[0].Object);
            _mockSectionRepository
                .Setup(x => x.AllSectionsByRouteIdsAsync(default))
                .ReturnsAsync(new Dictionary<ObjectId, List<Section>>
                {
                    { routeId, sections }
                });
            _sut = new SectionLogicProvider(
                _mockRepositoryManager.Object,
                _mockSectionLogicFactory.Object,
                _mockDomainEvents.Object,
                _cache,
                _mockLogger);

            // Act
            var result = (await _sut.GetAllAsync()).ToList();

            // Assert
            foreach (var entry in result)
            {
                Assert.Contains(entry.Value[0].Origin, s => s.StationId == sections[0].Origin[0]);
                Assert.Contains(entry.Value[0].SectionOnly, s => s.StationId == sections[0].SectionOnly[0]);
                Assert.Contains(entry.Value[0].Destination, s => s.StationId == sections[0].Destination[0]);
            }
        }

        [Fact]
        public async Task GetByRouteIdAsync_WhenCalled_ReturnsCorrectValues()
        {
            var mockSectionLogics = new[]
            {
                new Mock<ISectionLogic>(),
            };
            var mockStationLogics = new[]
            {
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
                new Mock<IStationLogic>(),
            };
            var routeId = ObjectId.GenerateNewId();
            var sections = new List<Section>
            {
                new Section
                {
                    RouteId = routeId,
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
                }
            };

            mockSectionLogics[0]
                .SetupGet(x => x.Origin)
                .Returns(new List<IStationLogic> { mockStationLogics[0].Object });
            mockSectionLogics[0]
                .SetupGet(x => x.SectionOnly)
                .Returns(new List<IStationLogic> { mockStationLogics[1].Object });
            mockSectionLogics[0]
                .SetupGet(x => x.Destination)
                .Returns(new List<IStationLogic> { mockStationLogics[2].Object });
            mockStationLogics[0]
                .SetupGet(x => x.StationId)
                .Returns(sections[0].Origin[0]);
            mockStationLogics[1]
                .SetupGet(x => x.StationId)
                .Returns(sections[0].SectionOnly[0]);
            mockStationLogics[2]
                .SetupGet(x => x.StationId)
                .Returns(sections[0].Destination[0]);
            _mockSectionLogicCreator
                .SetupSequence(x => x.CreateAsync(default))
                .ReturnsAsync(mockSectionLogics[0].Object);
            _mockSectionRepository
                .Setup(x => x.AllSectionsByRouteIdsAsync(default))
                .ReturnsAsync(new Dictionary<ObjectId, List<Section>>
                {
                    { routeId, sections }
                });
            _sut = new SectionLogicProvider(
                _mockRepositoryManager.Object,
                _mockSectionLogicFactory.Object,
                _mockDomainEvents.Object,
                _cache,
                _mockLogger);

            // Act
            var result = (await _sut.GetByRouteIdAsync(routeId)).ToList();

            // Assert
            foreach (var item in result)
            {
                Assert.Contains(item.Origin, s => s.StationId == sections[0].Origin[0]);
                Assert.Contains(item.SectionOnly, s => s.StationId == sections[0].SectionOnly[0]);
                Assert.Contains(item.Destination, s => s.StationId == sections[0].Destination[0]);
            }
        }
    }
}

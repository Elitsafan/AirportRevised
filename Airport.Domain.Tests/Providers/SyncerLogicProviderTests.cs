using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Tests.Providers
{
    public class SyncerLogicProviderTests
    {
        #region Fields
        private readonly Mock<ISyncerLogicFactory> _mockSyncerFactory;
        private readonly Mock<ISyncerLogicCreator> _mockSyncerCreator;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly Mock<IRepositoryManager> _mockRepoManager;
        private readonly Mock<ISyncerRepository> _mockRepoSyncer;
        private readonly ILogger<SyncerLogicProvider> _mockLogger;
        private readonly MemoryCache _cache;
        #endregion

        public SyncerLogicProviderTests()
        {
            _mockSyncerFactory = new Mock<ISyncerLogicFactory>();
            _mockSyncerCreator = new Mock<ISyncerLogicCreator>();
            _mockDomainEvents = new Mock<IDomainEvents>();
            _mockRepoManager = new Mock<IRepositoryManager>();
            _mockRepoSyncer = new Mock<ISyncerRepository>();
            _mockLogger = Mock.Of<ILogger<SyncerLogicProvider>>();
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        [Fact]
        public async Task GetAllAsync_WhenCalled_ReturnsCorrectValues()
        {
            // Arrange
            var syncer = new Syncer
            {
                SyncerId = ObjectId.GenerateNewId()
            };
            var mockSyncerLogic = new Mock<ISyncerLogic>();
            mockSyncerLogic
                .SetupGet(x => x.SyncerId)
                .Returns(syncer.SyncerId);
            _mockSyncerFactory
                .Setup(x => x.GetCreator(It.IsAny<Syncer>()))
                .Returns(_mockSyncerCreator.Object);
            _mockSyncerCreator
                .Setup(x => x.Create())
                .Returns(mockSyncerLogic.Object);
            _mockRepoManager
                .SetupGet(x => x.SyncerRepository)
                .Returns(_mockRepoSyncer.Object);
            _mockRepoSyncer
                .Setup(x => x.GetAllAsync(default))
                .ReturnsAsync(new[] { syncer });

            var sut = new SyncerLogicProvider(
                _mockRepoManager.Object,
                _mockSyncerFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            var result = (await sut.GetAllAsync()).ToList();

            // Assert
            Assert.Collection(result, s => Assert.Equal(mockSyncerLogic.Object, s));
        }

        [Fact]
        public async Task GetByIdAsync_WhenCalled_ReturnsCorrectValues()
        {
            // Arrange
            var syncer = new Syncer
            {
                SyncerId = ObjectId.GenerateNewId()
            };
            var mockSyncerLogic = new Mock<ISyncerLogic>();
            mockSyncerLogic
                .SetupGet(x => x.SyncerId)
                .Returns(syncer.SyncerId);
            _mockSyncerFactory
                .Setup(x => x.GetCreator(It.IsAny<Syncer>()))
                .Returns(_mockSyncerCreator.Object);
            _mockSyncerCreator
                .Setup(x => x.Create())
                .Returns(mockSyncerLogic.Object);
            _mockRepoManager
                .SetupGet(x => x.SyncerRepository)
                .Returns(_mockRepoSyncer.Object);
            _mockRepoSyncer
                .Setup(x => x.GetAllAsync(default))
                .ReturnsAsync(new[] { syncer });
            _mockRepoSyncer
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>(), default))
                .ReturnsAsync(syncer);

            var sut = new SyncerLogicProvider(
                _mockRepoManager.Object,
                _mockSyncerFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            var result = await sut.GetByIdAsync(syncer.SyncerId);

            // Assert
            Assert.Equal(syncer.SyncerId, result.SyncerId);
        }
    }
}

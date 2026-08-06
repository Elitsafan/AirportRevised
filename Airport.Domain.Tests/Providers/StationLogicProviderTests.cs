using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Tests.Providers
{
    public class StationLogicProviderTests
    {
        #region Fields
        private StationLogicProvider? _sut;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IRouteRepository> _mockRouteRepository;
        private readonly Mock<IStationRepository> _mockStationRepository;
        private readonly Mock<ITrafficLightRepository> _mockTrafficLightRepository;
        private readonly MemoryCache _cache;
        private readonly ILogger<StationLogicProvider> _mockLogger;
        private readonly Mock<IStationLogicFactory> _mockStationLogicFactory;
        private readonly Mock<IStationLogicCreator> _mockStationLogicCreator;
        private readonly Mock<IStationLogic> _mockStationLogic1;
        private readonly Mock<IStationLogic> _mockStationLogic2;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        #endregion

        public StationLogicProviderTests()
        {
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockStationRepository = new Mock<IStationRepository>();
            _mockTrafficLightRepository = new Mock<ITrafficLightRepository>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = Mock.Of<ILogger<StationLogicProvider>>();
            _mockStationLogicFactory = new Mock<IStationLogicFactory>();
            _mockStationLogicCreator = new Mock<IStationLogicCreator>();
            _mockStationLogic1 = new Mock<IStationLogic>();
            _mockStationLogic2 = new Mock<IStationLogic>();
            _mockDomainEvents = new Mock<IDomainEvents>();

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepository.Object);
            _mockRepositoryManager
                .SetupGet(x => x.StationRepository)
                .Returns(_mockStationRepository.Object);
            _mockRepositoryManager
                .SetupGet(x => x.TrafficLightRepository)
                .Returns(_mockTrafficLightRepository.Object);
            _mockStationLogicFactory
                .Setup(x => x.GetCreator(It.IsAny<Station>()))
                .Returns(_mockStationLogicCreator.Object);
            _mockStationLogicCreator
                .SetupSequence(x => x.Create())
                .Returns(_mockStationLogic1.Object)
                .Returns(_mockStationLogic2.Object);
        }

        [Fact]
        public async Task GetByRouteIdAsync_WhenCalled_ReturnsCorrectValues()
        {
            // Arrange
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                RouteName = "TestRoute",
                Directions = new List<Direction>
                {
                    new Direction
                    {
                        From = ObjectId.GenerateNewId(),
                        To = ObjectId.GenerateNewId()
                    }
                }
            };

            var stations = new List<Station>
            {
                new Station
                {
                    StationId = route.Directions[0].From,
                    EstimatedWaitingTime = TimeSpan.FromSeconds(123)
                },
                new Station
                {
                    StationId = route.Directions[0].To,
                    EstimatedWaitingTime = TimeSpan.FromSeconds(456)
                }
            };

            _mockRouteRepository
                .Setup(x => x.GetByIdAsync(route.RouteId, default))
                .ReturnsAsync(route);
            _mockStationRepository
                .Setup(x => x.GetAllAsync(default))
                .ReturnsAsync(stations);
            _mockStationRepository
                .Setup(x => x.GetStationsByRouteIdAsync(route.RouteId, default))
                .ReturnsAsync(stations);
            _mockStationLogic1
                .SetupGet(x => x.StationId)
                .Returns(stations[0].StationId);
            _mockStationLogic2
                .SetupGet(x => x.StationId)
                .Returns(stations[1].StationId);

            _sut = new StationLogicProvider(
                _mockRepositoryManager.Object,
                _mockStationLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            var result = await _sut.GetByRouteIdAsync(route.RouteId);

            // Assert
            foreach (var item in result)
                Assert.Contains(item.StationId, stations.Select(s => s.StationId));
        }

        [Fact]
        public async Task GetByRouteIdAsync_NoStationsForProvidedRouteId_ThrowsLogicNotFoundException()
        {
            // Arrange
            var stations = new List<Station>
            {
                new()
                {
                    StationId = ObjectId.GenerateNewId(),
                    EstimatedWaitingTime = TimeSpan.FromSeconds(123)
                },
                new()
                {
                    StationId = ObjectId.GenerateNewId(),
                    EstimatedWaitingTime = TimeSpan.FromSeconds(456)
                }
            };

            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(stations);

            _sut = new StationLogicProvider(
                _mockRepositoryManager.Object,
                _mockStationLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act & Assert
            await Assert.ThrowsAsync<LogicNotFoundException>(() => _sut.GetByRouteIdAsync(ObjectId.Empty));
        }

        [Fact]
        public async Task GetByRouteIdAsync_RouteNotFound_ThrowsLogicNotFoundException()
        {
            // Arrange
            var stations = new List<Station>
            {
                new()
                {
                    StationId = ObjectId.GenerateNewId(),
                    EstimatedWaitingTime = TimeSpan.FromSeconds(123)
                },
                new()
                {
                    StationId = ObjectId.GenerateNewId(),
                    EstimatedWaitingTime = TimeSpan.FromSeconds(456)
                }
            };
            
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(stations);

            _sut = new StationLogicProvider(
                _mockRepositoryManager.Object,
                _mockStationLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act & Assert
            await Assert.ThrowsAsync<LogicNotFoundException>(() => _sut.GetByRouteIdAsync(ObjectId.Empty));
        }
    }
}

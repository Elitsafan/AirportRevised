using Airport.Models.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Tests.Providers
{
    public class StationLogicProviderTests
    {
        #region Fields
        private StationLogicProvider? _sut;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IRouteRepository> _mockRouteRepository;
        private readonly Mock<IStationRepository> _mockStationRepository;
        private readonly Mock<ITrafficLightRepository> _mockTrafficLightRepository;
        private readonly MemoryCache _cache;
        private readonly ILogger<StationLogicProvider> _mockLogger;
        private readonly Mock<IStationLogicFactory> _mockStationLogicFactory;
        private readonly Mock<IStationLogicCreator> _mockStationLogicCreator;
        private readonly Mock<IStationLogic> _mockStationLogic;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        #endregion

        public StationLogicProviderTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockStationRepository = new Mock<IStationRepository>();
            _mockTrafficLightRepository = new Mock<ITrafficLightRepository>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = Mock.Of<ILogger<StationLogicProvider>>();
            _mockStationLogicFactory = new Mock<IStationLogicFactory>();
            _mockStationLogicCreator = new Mock<IStationLogicCreator>();
            _mockStationLogic = new Mock<IStationLogic>();
            _mockDomainEvents = new Mock<IDomainEvents>();

            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(_mockScopeFactory.Object);
            _mockScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(_mockScope.Object);
            _mockScope
                .Setup(x => x.ServiceProvider)
                .Returns(_mockServiceProvider.Object);
            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IRepositoryManager)))
                .Returns(_mockRepositoryManager.Object);
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
                .Setup(x => x.Create())
                .Returns(_mockStationLogic.Object);
        }

        [Fact]
        public async Task GetAllAsync_WhenCalled_ReturnsCorrectValues()
        {
            // Arrange
            var station = new Station
            {
                StationId = ObjectId.GenerateNewId(),
                EstimatedWaitingTime = TimeSpan.FromSeconds(123)
            };
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Station> { station });
            _mockStationLogic
                .SetupGet(x => x.StationId)
                .Returns(station.StationId);
            _mockStationLogic
                .SetupGet(x => x.EstimatedWaitingTime)
                .Returns(station.EstimatedWaitingTime);
            _sut = new StationLogicProvider(
                _mockServiceProvider.Object,
                _mockStationLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            var result = await _sut.GetAllAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(station.StationId, result.First().StationId);
            Assert.Equal(station.EstimatedWaitingTime, result.First().EstimatedWaitingTime);
        }

        [Fact]
        public async Task GetStationLogicByIdAsync_StationNotExists_ThrowsLogicNotFoundException()
        {
            // Arrange
            var id = ObjectId.GenerateNewId();
            var station = new Station
            {
                StationId = ObjectId.GenerateNewId(),
                EstimatedWaitingTime = TimeSpan.FromSeconds(123)
            };
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Station> { station });

            _sut = new StationLogicProvider(
                _mockServiceProvider.Object,
                _mockStationLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<LogicNotFoundException>(
                () => _sut.GetStationLogicByIdAsync(id));
            Assert.Equal($"Station logic not found for Id: {id}", ex.Message);
        }

        [Fact]
        public async Task GetStationLogicByIdAsync_NoStationExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = ObjectId.GenerateNewId();

            _sut = new StationLogicProvider(
                _mockServiceProvider.Object,
                _mockStationLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.GetStationLogicByIdAsync(id));
            Assert.Equal("There are no stations.", ex.Message);
        }

        [Fact]
        public async Task GetStationLogicByIdAsync_WhenCalled_ReturnsCorrectValues()
        {
            // Arrange
            var station = new Station
            {
                StationId = ObjectId.GenerateNewId(),
                EstimatedWaitingTime = TimeSpan.FromSeconds(123)
            };
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Station> { station });
            _mockStationLogic
                .SetupGet(x => x.StationId)
                .Returns(station.StationId);

            _sut = new StationLogicProvider(
                _mockServiceProvider.Object,
                _mockStationLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            var actual = await _sut.GetStationLogicByIdAsync(station.StationId);

            // Assert
            Assert.Equal(_mockStationLogic.Object.StationId, actual.StationId);
            Assert.Equal(_mockStationLogic.Object.EstimatedWaitingTime, actual.EstimatedWaitingTime);
        }

        [Fact]
        public async Task FindByRouteIdAsync_WhenCalled_ReturnsCorrectValues()
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
                .Setup(x => x.GetRouteByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(route);
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(stations);
            _mockStationRepository
                .Setup(x => x.GetStationsByRouteAsync(It.IsAny<Route>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stations);
            _sut = new StationLogicProvider(
                _mockServiceProvider.Object,
                _mockStationLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            var result = await _sut.FindStationLogicsByRouteIdAsync(route.RouteId);

            // Assert
            foreach (var item in result)
                Assert.Contains(item.StationId, stations.Select(s => s.StationId));
        }

        [Fact]
        public async Task FindStationLogicsByRouteIdAsync_RouteNotFound_ThrowsLogicProvisionFailedException()
        {
            // Arrange
            var stations = new List<Station>
            {
                new Station
                {
                    StationId = ObjectId.GenerateNewId(),
                    EstimatedWaitingTime = TimeSpan.FromSeconds(123)
                },
                new Station
                {
                    StationId = ObjectId.GenerateNewId(),
                    EstimatedWaitingTime = TimeSpan.FromSeconds(456)
                }
            };
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(stations);
            _mockRouteRepository
                .Setup(x => x.GetRouteByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityNotFoundException());
            _sut = new StationLogicProvider(
                _mockServiceProvider.Object,
                _mockStationLogicFactory.Object,
                _cache,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act & Assert
            await Assert.ThrowsAsync<LogicProvisionFailedException>(
                () => _sut.FindStationLogicsByRouteIdAsync(ObjectId.Empty));
        }
    }
}

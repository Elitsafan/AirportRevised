using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Airport.Domain.Tests.Providers
{
    public class StationLogicProviderTests
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StationLogicProvider> _loggerStationLogicProvider;
        private readonly IMemoryCache _cache;
        private readonly Mock<IStationLogicFactory> _mockStationLogicFactory;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IStationRepository> _mockStationRepository;
        private readonly Mock<IRouteRepository> _mockRouteRepository;
        private readonly Mock<IStationLogicCreator> _mockStationLogicCreator;
        private readonly Mock<IStationLogic> _mockStationLogic;
        private Station _station;
        #endregion

        public StationLogicProviderTests()
        {
            _loggerStationLogicProvider = Mock.Of<ILogger<StationLogicProvider>>();
            _mockStationLogicFactory = new Mock<IStationLogicFactory>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockStationRepository = new Mock<IStationRepository>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockStationLogicCreator = new Mock<IStationLogicCreator>();
            _mockStationLogic = new Mock<IStationLogic>();
            _station = new Station
            {
                StationId = ObjectId.GenerateNewId(),
            };

            _mockRepositoryManager
                .SetupGet(x => x.StationRepository)
                .Returns(_mockStationRepository.Object);
            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepository.Object);
            _mockStationLogicFactory
                .Setup(x => x.GetCreator(_station))
                .Returns(_mockStationLogicCreator.Object);
            _mockStationLogicCreator
                .Setup(x => x.Create())
                .Returns(_mockStationLogic.Object);
            _mockStationLogic
                .SetupGet(x => x.StationId)
                .Returns(_station.StationId);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IRepositoryManager>(factory => _mockRepositoryManager.Object);
            serviceCollection.AddSingleton<IStationLogicFactory>(_mockStationLogicFactory.Object);
            serviceCollection.AddMemoryCache(options =>
             {
                 options.SizeLimit = 1024;
             });
            _serviceProvider = serviceCollection.BuildServiceProvider();
            _cache = _serviceProvider.GetRequiredService<IMemoryCache>();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsNotNullAsync()
        {
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([_station]);
            var stationLogicProvider = await StationLogicProvider.CreateAsync(
                _serviceProvider, 
                _cache,
                _loggerStationLogicProvider);
            var result = await stationLogicProvider.GetAllAsync();

            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task Created_WithNoStations_ThrowsInvalidOperationExceptionAsync() => 
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => StationLogicProvider.CreateAsync(
                    _serviceProvider,
                    _cache,
                    _loggerStationLogicProvider));

        [Fact]
        public async Task GetStationLogicByIdAsync_StationLogicNotFound_ThrowsLogicNotFoundExceptionAsync()
        {
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([_station]);

            var stationLogicProvider = await StationLogicProvider.CreateAsync(
                _serviceProvider,
                _cache,
                _loggerStationLogicProvider);
            await Assert.ThrowsAsync<LogicNotFoundException>(
                () => stationLogicProvider.GetStationLogicByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task GetStationLogicByIdAsync_ReturnsValidValueAsync()
        {
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([_station]);
            _mockStationLogic
                .SetupGet(x => x.StationId)
                .Returns(_station.StationId);

            var stationLogicProvider = await StationLogicProvider.CreateAsync(
                _serviceProvider,
                _cache,
                _loggerStationLogicProvider);
            var actual = await stationLogicProvider.GetStationLogicByIdAsync(_station.StationId);

            Assert.Equal(_mockStationLogic.Object.StationId, actual.StationId);
        }

        [Fact]
        public async Task FindByRouteIdAsync_WhenCalled_ReturnsValueAsync()
        {
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([_station]);
            _mockStationRepository
                .Setup(x => x.GetStationsByRouteAsync(It.IsAny<Route>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([_station]);
            var stationLogicProvider = await StationLogicProvider.CreateAsync(
                _serviceProvider,
                _cache,
                _loggerStationLogicProvider);
            var result = await stationLogicProvider.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>());

            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task FindByRouteIdAsync_RouteNotFound_ThrowsArgumentExceptionAsync()
        {
            _mockStationRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([_station]);
            _mockStationRepository
                .Setup(x => x.GetStationsByRouteAsync(It.IsAny<Route>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentNullException());
            _mockRouteRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(It.IsAny<Route>());
                
            var stationLogicProvider = await StationLogicProvider.CreateAsync(
                _serviceProvider,
                _cache,
                _loggerStationLogicProvider);
            await Assert.ThrowsAsync<ArgumentException>(
                () => stationLogicProvider.FindStationLogicsByRouteIdAsync(It.IsAny<ObjectId>()));
        }
    }
}

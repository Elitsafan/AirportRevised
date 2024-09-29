using Airport.Contracts.Database;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;

namespace Airport.Services.Tests
{
    public class AirportServiceTests
    {
        #region Fields
        private IServiceProvider _serviceProvider;
        private Mock<IRepositoryManager> _mockRepositoryManager;
        private Mock<IStationRepository> _mockStationRepository;
        private Mock<IFlightRepository> _mockFlightRepository;
        private Mock<IRouteRepository> _mockRouteRepository;
        private Mock<IMapper> _mockMapper;
        private Mock<IAirportHubService> _mockHubService;
        private Mock<IFlightLogicFactory> _mockFlightLogicFactory;
        private Mock<IDirectionLogicFactory> _mockDirectionLogicFactory;
        private Mock<IStationLogicFactory> _mockStationLogicFactory;
        private Mock<IRouteLogicFactory> _mockRouteLogicFactory;
        private Mock<IDirectionLogicProvider> _mockDirectionLogicProvider;
        private Mock<IStationLogicProvider> _mockStationLogicProvider;
        private Mock<IRouteLogicProvider> _mockRouteLogicProvider;
        private Mock<IAirportDbConfiguration> _mockAirportDbConfiguration;
        private Mock<IMongoClient> _mockMongoClient;
        private Mock<IMemoryCache> _mockCache;
        private Mock<ICacheEntry> _mockCacheEntry;
        private ILogger<AirportService> _mockLogger;
        #endregion

        public AirportServiceTests()
        {
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockStationRepository = new Mock<IStationRepository>();
            _mockFlightRepository = new Mock<IFlightRepository>();
            _mockRouteRepository = new Mock<IRouteRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockHubService = new Mock<IAirportHubService>();
            _mockFlightLogicFactory = new Mock<IFlightLogicFactory>();
            _mockDirectionLogicFactory = new Mock<IDirectionLogicFactory>();
            _mockStationLogicFactory = new Mock<IStationLogicFactory>();
            _mockRouteLogicFactory = new Mock<IRouteLogicFactory>();
            _mockDirectionLogicProvider = new Mock<IDirectionLogicProvider>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
            _mockRouteLogicProvider = new Mock<IRouteLogicProvider>();
            _mockAirportDbConfiguration = new Mock<IAirportDbConfiguration>();
            _mockMongoClient = new Mock<IMongoClient>();
            _mockCache = new Mock<IMemoryCache>();
            _mockCacheEntry = new Mock<ICacheEntry>();
            _mockLogger = Mock.Of<ILogger<AirportService>>();
            _mockCache
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(_mockCacheEntry.Object);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IAirportHubService>(_mockHubService.Object);
            serviceCollection.AddSingleton<IFlightLogicFactory>(_mockFlightLogicFactory.Object);
            serviceCollection.AddSingleton<IDirectionLogicFactory>(_mockDirectionLogicFactory.Object);
            serviceCollection.AddSingleton<IStationLogicFactory>(_mockStationLogicFactory.Object);
            serviceCollection.AddSingleton<IRouteLogicFactory>(_mockRouteLogicFactory.Object);
            serviceCollection.AddSingleton<IDirectionLogicProvider>(_mockDirectionLogicProvider.Object);
            serviceCollection.AddSingleton<IStationLogicProvider>(_mockStationLogicProvider.Object);
            serviceCollection.AddSingleton<IRouteLogicProvider>(_mockRouteLogicProvider.Object);
            serviceCollection.AddSingleton<IAirportDbConfiguration>(_mockAirportDbConfiguration.Object);
            serviceCollection.AddSingleton<IMongoClient>(_mockMongoClient.Object);
            serviceCollection.AddSingleton<ILogger<AirportService>>(_mockLogger);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public async Task GetStatusAsync_WhenCalled_ReturnsNotNullValueAsync()
        {
            var route = new Route();
            var stationDto = new StationDTO();
            var routeDto = new RouteDTO();

            _mockRepositoryManager
                .SetupGet(x => x.StationRepository)
                .Returns(_mockStationRepository.Object);
            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepository.Object);
            _mockRouteRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Route[] { route });
            _mockStationLogicProvider
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IStationLogic[] { Mock.Of<IStationLogic>() });
            _mockMapper
                .Setup(x => x.Map<StationDTO>(It.IsAny<IStationLogic>()))
                .Returns(() => stationDto);
            _mockMapper
                .Setup(x => x.Map<RouteDTO>(It.IsAny<Route>()))
                .Returns(() => routeDto);

            var airportService = new AirportService(
                _serviceProvider,
                _mockStationLogicProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockLogger);

            var actual = await airportService.GetStatusAsync();
            var expected = new AirportStatus
            {
                Stations = new List<StationDTO> { stationDto },
                Routes = new List<RouteDTO> { routeDto },
            };
            Assert.Equivalent(expected, actual);
        }

        [Fact]
        public async Task StartAsync_WhenFirstStarted_ReturnsStartedStringAsync()
        {
            var airportService = new AirportService(
                _serviceProvider,
                _mockStationLogicProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockLogger);

            var actual = await airportService.StartAsync();

            Assert.True("Started" == actual);
        }

        [Fact]
        public async Task StartAsync_WhenStarted_ReturnsAlreadyStartedStringAsync()
        {
            var airportService = new AirportService(
                _serviceProvider,
                _mockStationLogicProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockLogger);

            await airportService.StartAsync();
            object expected = true;

            _mockCache
                .Setup(x => x.TryGetValue(It.IsAny<string>(), out expected!))
                .Returns(true);
            Assert.True(airportService.HasStarted);

            var actual = await airportService.StartAsync();

            Assert.True("Already started" == actual);
        }

        [Fact]
        public async Task GetSummaryAsync_WhenCalled_ReturnsSummaryAsync()
        {
            var departure = new Departure { FlightId = ObjectId.GenerateNewId() };
            var landing = new Landing { FlightId = ObjectId.GenerateNewId() };

            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(_mockFlightRepository.Object);
            _mockFlightRepository
                .Setup(x => x.OrderByEntranceAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Flight[] { departure, landing });

            var airportService = new AirportService(
                _serviceProvider,
                _mockStationLogicProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockLogger);

            var actual = await airportService.GetPagedSummaryAsync(new GetSummaryParameters
            {
                PageNumber = 1,
                PageSize = 1,
            });
            IPagedList<FlightSummary> expected = new List<FlightSummary>
            {
                new FlightSummary
                {
                    FlightId = departure.FlightId,
                    Stations = new List<OccupationDetails>(),
                    FlightType = FlightType.Departure
                },
                new FlightSummary
                {
                    FlightId = landing.FlightId,
                    Stations = new List<OccupationDetails>(),
                    FlightType = FlightType.Landing
                }
            }.ToPagedList(1, 1);
            Assert.Equivalent(expected, actual);
            Assert.True(expected.SequenceEqual(actual));
        }
    }
}
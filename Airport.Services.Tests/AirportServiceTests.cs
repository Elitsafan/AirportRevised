using Airport.Persistence;
using Airport.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;

namespace Airport.Services.Tests
{
    public class AirportServiceTests
    {
        #region Fields
        private readonly Mock<IAirportStateProvider> _mockAirportStateProvider;
        private readonly Mock<IStationLogicProvider> _mockStationLogicProvider;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ILogger<AirportService> _mockLogger;
        #endregion

        public AirportServiceTests()
        {
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockMapper = new Mock<IMapper>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
            _mockLogger = Mock.Of<ILogger<AirportService>>();
            _mockAirportStateProvider = new Mock<IAirportStateProvider>();
        }

        [Fact]
        public async Task GetStatusAsync_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            var route = new Route();
            var stationDto = new StationDTO();
            var routeDto = new RouteDTO();
            var mockStationRepository = new Mock<IStationRepository>();
            var mockRouteRepository = new Mock<IRouteRepository>();

            _mockRepositoryManager
                .SetupGet(x => x.StationRepository)
                .Returns(mockStationRepository.Object);
            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(mockRouteRepository.Object);
            mockRouteRepository
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
                _mockAirportStateProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            // Act
            var actual = await airportService.GetStatusAsync();
            var expected = new AirportStatus
            {
                Stations = new List<StationDTO> { stationDto },
                Routes = new List<RouteDTO> { routeDto },
            };

            // Assert
            Assert.Equivalent(expected, actual);
        }

        [Fact]
        public async Task StartAsync_WhenFirstStarted_ReturnsCorrectValue()
        {
            // Arrange
            var startLock = new Microsoft.VisualStudio.Threading.AsyncSemaphore(1);
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(false);
            _mockAirportStateProvider
                .SetupGet(x => x.StartLock)
                .Returns(startLock);
            var airportService = new AirportService(
                _mockAirportStateProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            // Act
            var actual = await airportService.StartAsync();

            // Assert
            Assert.True("Started" == actual);
        }

        [Fact]
        public async Task StartAsync_WhenAlreadyStarted_ReturnsCorrectValue()
        {
            // Arrange
            var startLock = new Microsoft.VisualStudio.Threading.AsyncSemaphore(1);
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            _mockAirportStateProvider
                .SetupGet(x => x.StartLock)
                .Returns(startLock);
            var airportService = new AirportService(
                _mockAirportStateProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            // Act
            var actual = await airportService.StartAsync();

            // Assert
            Assert.True("Already started" == actual);
        }

        [Fact]
        public async Task GetSummaryAsync_WhenCalled_ReturnsSummary()
        {
            // Arrange
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            var departure = new Departure { FlightId = ObjectId.GenerateNewId() };
            var landing = new Landing { FlightId = ObjectId.GenerateNewId() };
            var mockFlightRepository = new Mock<IFlightRepository>();
            var summary = new SummaryWithMetadata
            {
                Summary = new List<FlightSummary>
                {
                    new()
                    {
                        FlightId = departure.FlightId,
                        Stations = new List<OccupationDetails>(),
                        FlightType = FlightType.Departure
                    },
                    new()
                    {
                        FlightId = landing.FlightId,
                        Stations = new List<OccupationDetails>(),
                        FlightType = FlightType.Landing
                    }
                }.ToPagedList(1, 2),
                DeparturesCount = 1,
                LandingsCount = 1
            };

            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);
            mockFlightRepository
                .Setup(x => x.OrderByEntranceAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Flight[] { departure, landing });

            var airportService = new AirportService(
                _mockAirportStateProvider.Object,
                _mockStationLogicProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            // Act
            var actual = await airportService.GetSummaryWithMetadataAsync(new GetSummaryParameters
            {
                PageNumber = 1,
                PageSize = 2,
            });

            // Assert
            Assert.Equal(summary.LandingsCount, actual.LandingsCount);
            Assert.Equal(summary.DeparturesCount, actual.DeparturesCount);
            Assert.Equal(summary.Summary, actual.Summary);
        }
    }
}
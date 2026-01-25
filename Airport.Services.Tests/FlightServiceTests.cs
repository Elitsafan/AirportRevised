using Airport.Domain.Exceptions;
using Airport.Models.DTOs;

namespace Airport.Services.Tests
{
    public class FlightServiceTests
    {
        #region Fields
        private readonly Mock<IAirportStateProvider> _mockAirportStateProvider;
        private readonly Mock<IFlightLogicFactory> _mockFlightLogicFactory;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IAirportHubService> _mockAirportHubService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IFlightLogicCreator> _mockFlightLogicCreator;
        private readonly Mock<IFlightLogic> _mockFlightLogic;
        private readonly ILogger<FlightService> _mockLogger;
        private FlightService _flightService;
        #endregion

        public FlightServiceTests()
        {
            _mockAirportStateProvider = new Mock<IAirportStateProvider>();
            _mockFlightLogicFactory = new Mock<IFlightLogicFactory>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockAirportHubService = new Mock<IAirportHubService>();
            _mockMapper = new Mock<IMapper>();
            _mockFlightLogicCreator = new Mock<IFlightLogicCreator>();
            _mockFlightLogic = new Mock<IFlightLogic>();
            _mockLogger = Mock.Of<ILogger<FlightService>>();
            _flightService = null!;
        }

        [Fact]
        public async Task AddFlightAsync_FlightForCreationIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _flightService.AddFlightAsync(It.IsAny<ObjectId>(), null!));
        }

        [Fact]
        public async Task GetFlightByIdAsync_NotExist_ReturnsNull()
        {
            // Assert
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            var mockFlightRepository = new Mock<IFlightRepository>();

            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);
            mockFlightRepository
                .Setup(x => x.GetFlightByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityNotFoundException());

            // Assert
            Assert.Null(await _flightService.GetFlightByIdAsync(It.IsAny<ObjectId>()));
        }

        [Fact]
        public async Task GetFlightByIdAsync_WhenCalled_ReturnsCorrectFlight()
        {
            // Arrange
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            var mockFlightRepository = new Mock<IFlightRepository>();
            var departure = new Departure();
            var departureDto = new DepartureDTO();

            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);
            mockFlightRepository
                .Setup(x => x.GetFlightByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(departure);
            _mockFlightLogicCreator
                .Setup(x => x.CreateAsync())
                .ReturnsAsync(_mockFlightLogic.Object);
            _mockMapper
                .Setup(x => x.Map<FlightDTO>(It.IsAny<Flight>()))
                .Returns(departureDto);

            // Assert
            Assert.NotNull(await _flightService.GetFlightByIdAsync(It.IsAny<ObjectId>()));
        }

        [Fact]
        public async Task AddFlightAsync_FlightIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _flightService.AddFlightAsync(It.IsAny<ObjectId>(), null!));
        }

        [Fact]
        public async Task AddFlightAsync_WhenCalled_ShouldCallMapperMapOnce()
        {
            // Assert
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            var mockFlightRepository = new Mock<IFlightRepository>();
            var flightForCreationDto = new DepartureForCreationDTO();
            var departure = new Departure();

            _mockFlightLogicFactory
                .Setup(x => x.GetCreatorAsync(departure, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockFlightLogicCreator.Object);
            _mockFlightLogicCreator
                .Setup(x => x.CreateAsync())
                .ReturnsAsync(_mockFlightLogic.Object);
            _mockMapper
                .Setup(x => x.Map<Flight>(flightForCreationDto))
                .Returns(departure)
                .Verifiable();
            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);
            mockFlightRepository
                .Setup(x => x.UpdateFlightAsync(It.IsAny<Flight>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(UpdateResult.Modified);

            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            // Act
            await _flightService.AddFlightAsync(It.IsAny<ObjectId>(), flightForCreationDto);

            // Assert
            _mockMapper.Verify();
        }

        [Fact]
        public async Task DeleteFlightAsync_WheanCalled_ReturnsTrue()
        {
            // Arrange
            var mockFlightRepository = new Mock<IFlightRepository>();
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);
            mockFlightRepository
                .Setup(x => x.DeleteOneAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            // Assert
            Assert.True(await _flightService.DeleteFlightAsync(ObjectId.Empty));
        }

        [Fact]
        public async Task DeleteFlightAsync_FlightNotExists_ReturnsFalse()
        {
            // Assert
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            var mockFlightRepository = new Mock<IFlightRepository>();
            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);
            mockFlightRepository
                .Setup(x => x.DeleteOneAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            // Assert
            Assert.False(await _flightService.DeleteFlightAsync(ObjectId.Empty));
        }
    }
}

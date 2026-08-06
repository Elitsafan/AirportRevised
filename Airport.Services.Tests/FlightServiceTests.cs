using Airport.Domain.Exceptions;
using Airport.Models.DTOs;
using Airport.Services.Services;

namespace Airport.Services.Tests
{
    public class FlightServiceTests
    {
        #region Fields
        private readonly Mock<IAirportStateProvider> _mockAirportStateProvider;
        private readonly Mock<IFlightLogicFactory> _mockFlightLogicFactory;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IFlightQueue> _mockFlightQueue;
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
            _mockMapper = new Mock<IMapper>();
            _mockFlightQueue = new Mock<IFlightQueue>();
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
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockFlightQueue.Object,
                _mockLogger);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _flightService.AddFlightAsync(null!));
        }

        [Fact]
        public async Task GetByIdAsync_NotExist_ThrowsEntityNotFoundException()
        {
            // Assert
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            var mockFlightRepository = new Mock<IFlightRepository>();

            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);

            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockFlightQueue.Object,
                _mockLogger);

            mockFlightRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityNotFoundException());

            // Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _flightService.GetFlightByIdAsync(It.IsAny<ObjectId>()));
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
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockFlightQueue.Object,
                _mockLogger);

            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);
            mockFlightRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(departure);
            _mockFlightLogicCreator
                .Setup(x => x.Create())
                .Returns(_mockFlightLogic.Object);
            _mockMapper
                .Setup(x => x.Map<FlightDTO>(It.IsAny<Flight>()))
                .Returns(departureDto);

            // Act & Assert
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
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockFlightQueue.Object,
                _mockLogger);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _flightService.AddFlightAsync(null!));
        }

        [Fact]
        public async Task AddFlightAsync_WhenCalled_ShouldCallMapperMapOnce()
        {
            // Arrange
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
                .Setup(x => x.Create())
                .Returns(_mockFlightLogic.Object);
            _mockMapper
                .Setup(x => x.Map<Flight>(flightForCreationDto))
                .Returns(departure)
                .Verifiable();
            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);

            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockFlightQueue.Object,
                _mockLogger);

            // Act
            await _flightService.AddFlightAsync(flightForCreationDto);

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
                .Setup(x => x.DeleteOneAsync(It.IsAny<ObjectId>(), null, default))
                .ReturnsAsync(true);

            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockFlightQueue.Object,
                _mockLogger);

            // Act & Assert
            Assert.True(await _flightService.DeleteFlightAsync(ObjectId.Empty));
        }

        [Fact]
        public async Task DeleteFlightAsync_FlightNotExists_ReturnsFalse()
        {
            // Arrange
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            var mockFlightRepository = new Mock<IFlightRepository>();

            _mockRepositoryManager
                .SetupGet(x => x.FlightRepository)
                .Returns(mockFlightRepository.Object);

            mockFlightRepository
                .Setup(x => x.DeleteOneAsync(It.IsAny<ObjectId>(), null, default))
                .ReturnsAsync(false);

            _flightService = new FlightService(
                _mockAirportStateProvider.Object,
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockFlightQueue.Object,
                _mockLogger);

            // Act & Assert
            Assert.False(await _flightService.DeleteFlightAsync(ObjectId.Empty));
        }
    }
}

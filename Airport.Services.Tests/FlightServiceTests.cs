namespace Airport.Services.Tests
{
    public class FlightServiceTests
    {
        #region Fields
        private Mock<IFlightLogicFactory> _mockFlightLogicFactory;
        private Mock<IRepositoryManager> _mockRepositoryManager;
        private Mock<IAirportHubService> _mockAirportHubService;
        private Mock<IMapper> _mockMapper;
        private Mock<IFlightLogicCreator> _mockFlightLogicCreator;
        private Mock<IFlightLogic> _mockFlightLogic;
        private FlightService _flightService;
        private readonly ILogger<FlightService> _mockLogger;
        #endregion

        public FlightServiceTests()
        {
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
        public void FlightServiceCreated_NotNull()
        {
            _flightService = new FlightService(
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            Assert.NotNull(_flightService);
        }

        [Fact]
        public async Task ProcessFlightAsync_FlightForCreationIsNull_ThrowsArgumentNullExceptionAsync()
        {
            _flightService = new FlightService(
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _flightService.ProcessFlightAsync(It.IsAny<ObjectId>(), null!));
        }

        [Fact]
        public async Task ProcessFlightAsync_WhenCalled_ShouldCallMapperMapOnceAsync()
        {
            var mockFlightRepository = new Mock<IFlightRepository>();
            var flightForCreationDto = new DepartureForCreationDTO();
            var departure = new Departure();

            _mockFlightLogicFactory
                .Setup(x => x.GetCreator(departure))
                .Returns(_mockFlightLogicCreator.Object);
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
                .ReturnsAsync(true);

            _flightService = new FlightService(
                _mockFlightLogicFactory.Object,
                _mockRepositoryManager.Object,
                _mockAirportHubService.Object,
                _mockMapper.Object,
                _mockLogger);

            await _flightService.ProcessFlightAsync(It.IsAny<ObjectId>(), flightForCreationDto);

            _mockMapper.Verify();
        }
    }
}

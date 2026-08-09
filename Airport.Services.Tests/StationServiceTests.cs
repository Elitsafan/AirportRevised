using Airport.Contracts.Helpers;
using Airport.Services.Services;

namespace Airport.Services.Tests
{
    public class StationServiceTests
    {
        #region Fields
        private readonly IStationService _sut;
        private readonly Mock<IAirportStateProvider> _mockAirportStateProvider;
        private readonly Mock<IRepositoryManager> _mockRepoManager;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ILogger<StationService> _mockLogger;
        #endregion

        public StationServiceTests()
        {
            _mockAirportStateProvider = new Mock<IAirportStateProvider>();
            _mockRepoManager = new Mock<IRepositoryManager>();
            _mockDomainEvents = new Mock<IDomainEvents>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = Mock.Of<ILogger<StationService>>();

            _sut = new StationService(
                _mockAirportStateProvider.Object,
                _mockRepoManager.Object,
                _mockMapper.Object,
                _mockDomainEvents.Object,
                _mockLogger);
        }

        [Fact]
        public async Task AddStationAsync_WhenCalled_CorrectStationAdded()
        {
            // 
        }

    }
}

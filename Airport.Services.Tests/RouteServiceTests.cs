using Airport.Domain.Exceptions;
using Airport.Services.Abstractions;

namespace Airport.Services.Tests
{
    public class RouteServiceTests
    {
        #region Fields
        private Mock<IRouteLogicFactory> _mockRouteLogicFactory;
        private Mock<IRepositoryManager> _mockRepositoryManager;
        private Mock<IMapper> _mockMapper;
        private Mock<IRouteLogicCreator> _mockRouteLogicCreator;
        private Mock<IRouteLogic> _mockRouteLogic;
        private RouteService _routeService;
        private readonly ILogger<RouteService> _mockLogger;
        #endregion

        public RouteServiceTests()
        {
            _mockRouteLogicFactory = new Mock<IRouteLogicFactory>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockMapper = new Mock<IMapper>();
            _mockRouteLogicCreator = new Mock<IRouteLogicCreator>();
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockLogger = Mock.Of<ILogger<RouteService>>();
            _routeService = null!;
        }

        [Fact]
        public void RouteServiceCreated_NotNull()
        {
            _routeService = new RouteService(
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            Assert.NotNull(_routeService);
        }

        [Fact]
        public async Task GetAllRoutesAsync_WhenCalled_ReturnsAllRoutes()
        {
            var routeDto = new RouteDTO();
            var mockRouteRepository = new Mock<IRouteRepository>();
            var routes = new Route[]
            {
            new(),
            new(),
            new(),
            };

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(mockRouteRepository.Object);
            mockRouteRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(routes);
            _mockMapper
                .Setup(x => x.Map<RouteDTO>(It.IsAny<Route>()))
                .Returns(routeDto);

            _routeService = new RouteService(
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            Assert.NotEmpty(await _routeService.GetAllRoutesAsync().ToListAsync());
        }

        [Fact]
        public async Task GetRouteByIdAsync_NotExist_ReturnsNull()
        {
            var mockRouteRepository = new Mock<IRouteRepository>();

            _routeService = new RouteService(
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(mockRouteRepository.Object);
            mockRouteRepository
                .Setup(x => x.GetRouteByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityNotFoundException());

            Assert.Null(await _routeService.GetRouteByIdAsync(It.IsAny<ObjectId>()));
        }

        [Fact]
        public async Task GetRouteByIdAsync_WhenCalled_ReturnsCorrectRoute()
        {
            var mockRouteRepository = new Mock<IRouteRepository>();
            var route = new Route();
            var routeDto = new RouteDTO();

            _routeService = new RouteService(
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(mockRouteRepository.Object);
            mockRouteRepository
                .Setup(x => x.GetRouteByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(route);
            _mockRouteLogicCreator
                .Setup(x => x.CreateAsync())
                .ReturnsAsync(_mockRouteLogic.Object);
            _mockMapper
                .Setup(x => x.Map<RouteDTO>(It.IsAny<Route>()))
                .Returns(routeDto);

            Assert.NotNull(await _routeService.GetRouteByIdAsync(It.IsAny<ObjectId>()));
        }

        [Fact]
        public async Task UpdateRouteAsync_RouteIsCircular_ThrowsMissingRouteStationsException()
        {
            var mockStationRepository = new Mock<IStationRepository>();
            var route = new RouteForUpdateDTO
            {
                Directions = new List<DirectionDTO>
                {
                    new DirectionDTO
                    {
                        From = new ObjectId("000000000000000000000001"),
                        To = new ObjectId("000000000000000000000002")
                    }
                }
            };

            _mockRepositoryManager
                .SetupGet(x => x.StationRepository)
                .Returns(mockStationRepository.Object);

            _routeService = new RouteService(
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            await Assert.ThrowsAsync<MissingRouteStationsException>(
                () => _routeService.UpdateRouteAsync(
                    It.IsAny<ObjectId>(),
                    route,
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task UpdateRouteAsync_RouteIsCircular_ThrowsInvalidRouteStructureException()
        {
            var mockStationRepository = new Mock<IStationRepository>();
            var stationIds = new List<ObjectId>
            {
                new ObjectId("000000000000000000000001"),
                new ObjectId("000000000000000000000002"),
                new ObjectId("000000000000000000000003")
            };
            var route = new RouteForUpdateDTO
            {
                Directions = new List<DirectionDTO>
                {
                    new DirectionDTO
                    {
                        From = new ObjectId("000000000000000000000001"),
                        To = new ObjectId("000000000000000000000002")
                    },
                    new DirectionDTO
                    {
                        From = new ObjectId("000000000000000000000002"),
                        To = new ObjectId("000000000000000000000003")
                    },
                    new DirectionDTO
                    {
                        From = new ObjectId("000000000000000000000003"),
                        To = new ObjectId("000000000000000000000001")
                    },
                }
            };

            _mockRepositoryManager
                .SetupGet(x => x.StationRepository)
                .Returns(mockStationRepository.Object);
            mockStationRepository
                .Setup(x => x.GetExistingStationIdsAsync(
                    It.IsAny<IEnumerable<ObjectId>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(stationIds);

            _routeService = new RouteService(
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            await Assert.ThrowsAsync<InvalidRouteStructureException>(
                () => _routeService.UpdateRouteAsync(
                    It.IsAny<ObjectId>(), 
                    route, 
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task UpdateRouteAsync_RouteIsNull_ThrowsArgumentNullException()
        {
            _routeService = new RouteService(
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _routeService.UpdateRouteAsync(It.IsAny<ObjectId>(), null!, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task DeleteRouteAsync_WhenCalled_ReturnsTrue()
        {
            var mockRouteRepository = new Mock<IRouteRepository>();

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(mockRouteRepository.Object);
            mockRouteRepository
                .Setup(x => x.DeleteOneAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _routeService = new RouteService(
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            Assert.True(await _routeService.DeleteRouteAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task DeleteRouteAsync_RouteNotExists_ReturnsFalse()
        {
            var mockRouteRepository = new Mock<IRouteRepository>();

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(mockRouteRepository.Object);
            mockRouteRepository
                .Setup(x => x.DeleteOneAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _routeService = new RouteService(
                _mockRepositoryManager.Object,
                _mockMapper.Object,
                _mockLogger);

            Assert.False(await _routeService.DeleteRouteAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>()));
        }
    }
}
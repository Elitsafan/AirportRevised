using Airport.Contracts.Helpers;
using Airport.Domain.Exceptions;
using Airport.Services.Services;
using MongoDB.Driver;

namespace Airport.Services.Tests
{
    public class RouteServiceTests
    {
        #region Fields
        private readonly Mock<IRepositoryManager> _mockRepoManager;
        private readonly Mock<IStationRepository> _mockStationRepo;
        private readonly Mock<IRouteRepository> _mockRouteRepo;
        private readonly Mock<ISectionRepository> _mockSectionRepo;
        private readonly Mock<ISyncerRepository> _mockSyncerRepo;
        private readonly Mock<ITrafficLightRepository> _mockTrafficLightRepo;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMongoClient> _mockMongoClient;
        private readonly Mock<IClientSessionHandle> _mockClientSession;
        private readonly Mock<IRouteValidator> _mockRouteValidator;
        private readonly Mock<IRouteLogicCreator> _mockRouteLogicCreator;
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private readonly Mock<IAirportStateProvider> _mockAirportStateProvider;
        private readonly ILogger<RouteService> _mockLogger;
        private RouteService _routeService;
        #endregion

        public RouteServiceTests()
        {
            _mockAirportStateProvider = new Mock<IAirportStateProvider>();
            _mockRepoManager = new Mock<IRepositoryManager>();
            _mockRouteRepo = new Mock<IRouteRepository>();
            _mockStationRepo = new Mock<IStationRepository>();
            _mockSectionRepo = new Mock<ISectionRepository>();
            _mockSyncerRepo = new Mock<ISyncerRepository>();
            _mockTrafficLightRepo = new Mock<ITrafficLightRepository>();
            _mockDomainEvents = new Mock<IDomainEvents>();
            _mockMapper = new Mock<IMapper>();
            _mockMongoClient = new Mock<IMongoClient>();
            _mockClientSession = new Mock<IClientSessionHandle>();
            _mockRouteValidator = new Mock<IRouteValidator>();
            _mockRouteLogicCreator = new Mock<IRouteLogicCreator>();
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockLogger = Mock.Of<ILogger<RouteService>>();
            _routeService = null!;
        }

        [Fact]
        public async Task GetAllRoutesAsync_WhenCalled_ReturnsAllRoutes()
        {
            var routeDto = new RouteDTO();

            var routes = new Route[]
            {
                new(),
            };

            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            _mockRepoManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepo.Object);

            _mockRouteRepo
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(routes);

            _mockMapper
                .Setup(x => x.Map<RouteDTO>(It.IsAny<Route>()))
                .Returns(routeDto);

            _routeService = new RouteService(
                _mockAirportStateProvider.Object,
                _mockRepoManager.Object,
                _mockDomainEvents.Object,
                _mockMapper.Object,
                _mockMongoClient.Object,
                _mockRouteValidator.Object,
                _mockLogger);

            Assert.NotEmpty(await _routeService.GetAllRoutesAsync().ToListAsync());
        }

        [Fact]
        public async Task GetRouteByIdAsync_NotExist_ThrowsEntityNotFoundException()
        {
            _routeService = new RouteService(
                _mockAirportStateProvider.Object,
                _mockRepoManager.Object,
                _mockDomainEvents.Object,
                _mockMapper.Object,
                _mockMongoClient.Object,
                _mockRouteValidator.Object,
                _mockLogger);

            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            _mockRepoManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepo.Object);

            _mockRouteRepo
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityNotFoundException());

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _routeService.GetRouteByIdAsync(It.IsAny<ObjectId>()));
        }

        [Fact]
        public async Task GetRouteByIdAsync_WhenCalled_ReturnsCorrectRoute()
        {
            var route = new Route();

            var routeDto = new RouteDTO();

            _routeService = new RouteService(
                _mockAirportStateProvider.Object,
                _mockRepoManager.Object,
                _mockDomainEvents.Object,
                _mockMapper.Object,
                _mockMongoClient.Object,
                _mockRouteValidator.Object,
                _mockLogger);

            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            _mockRepoManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepo.Object);

            _mockRouteRepo
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(route);

            _mockRouteLogicCreator
                .Setup(x => x.CreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockRouteLogic.Object);

            _mockMapper
                .Setup(x => x.Map<RouteDTO>(It.IsAny<Route>()))
                .Returns(routeDto);

            Assert.NotNull(await _routeService.GetRouteByIdAsync(It.IsAny<ObjectId>()));
        }

        [Fact]
        public async Task UpdateRouteAsync_RouteIsCircular_ThrowsMissingRouteStationsException()
        {
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                Directions = new List<Direction>
                {
                    new Direction
                    {
                        From = new ObjectId("000000000000000000000001"),
                        To = new ObjectId("000000000000000000000002")
                    }
                }
            };

            var routeForUpdate = new RouteForUpdateDTO
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
                    }
                }
            };

            var updateRoute = new Route
            {
                RouteId = route.RouteId,
                Directions = new List<Direction>
                {
                    new Direction
                    {
                        From = new ObjectId("000000000000000000000001"),
                        To = new ObjectId("000000000000000000000002")
                    },
                    new Direction
                    {
                        From = new ObjectId("000000000000000000000002"),
                        To = new ObjectId("000000000000000000000003")
                    }
                }
            };

            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            _mockRouteValidator
                .Setup(x => x.ValidateRouteAsync(
                    It.IsAny<List<DirectionDTO>>(),
                    It.IsAny<Dictionary<ObjectId, int>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MissingRouteStationsException());

            _mockRepoManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.StationRepository)
                .Returns(_mockStationRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.SectionRepository)
                .Returns(_mockSectionRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.SyncerRepository)
                .Returns(_mockSyncerRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.TrafficLightRepository)
                .Returns(_mockTrafficLightRepo.Object);

            _mockMapper
                .Setup(x => x.Map<Route>(routeForUpdate))
                .Returns(updateRoute);

            _mockMapper
                .Setup(x => x.Map<List<Section>>(It.IsAny<HashSet<DirectionDTO>>()))
                .Returns(new List<Section>());

            _mockMongoClient
                .Setup(x => x.StartSessionAsync(null, default))
                .ReturnsAsync(_mockClientSession.Object);

            _mockRouteRepo
                .Setup(x => x.UpdateRouteAsync(It.IsAny<Route>(), It.IsAny<IClientSessionHandle>(), false, default))
                .ReturnsAsync(Models.Enums.UpdateResult.Modified);

            _mockSectionRepo
                .Setup(x => x.DeleteByRouteIdAsync(It.IsAny<ObjectId>(), _mockClientSession.Object, default))
                .ReturnsAsync(true);

            _mockStationRepo
                .SetupSequence(x => x.GetCommonIdsToCountsAsync(
                    It.IsAny<IEnumerable<ObjectId>>(),
                    It.IsAny<IEnumerable<ObjectId>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<ObjectId, int>
                {
                    { route.Directions[0].To, 1 },
                    { route.Directions[0].From, 1 },
                })
                .ReturnsAsync(new Dictionary<ObjectId, int>
                {
                    { updateRoute.Directions[0].To, 1 },
                    { updateRoute.Directions[0].From, 2 },
                    { updateRoute.Directions[1].To, 1 },
                });

            _routeService = new RouteService(
                _mockAirportStateProvider.Object,
                _mockRepoManager.Object,
                _mockDomainEvents.Object,
                _mockMapper.Object,
                _mockMongoClient.Object,
                _mockRouteValidator.Object,
                _mockLogger);

            await Assert.ThrowsAsync<MissingRouteStationsException>(
                () => _routeService.UpdateRouteAsync(
                    It.IsAny<ObjectId>(),
                    routeForUpdate,
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task UpdateRouteAsync_RouteIsCircular_ThrowsInvalidRouteStructureException()
        {
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId()
            };

            var routeForUpdate = new RouteForUpdateDTO
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

            var stationIds = new List<ObjectId>
            {
                new ObjectId("000000000000000000000001"),
                new ObjectId("000000000000000000000002"),
                new ObjectId("000000000000000000000003")
            };

            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            _mockRouteValidator
                .Setup(x => x.ValidateRouteAsync(
                    It.IsAny<List<DirectionDTO>>(),
                    It.IsAny<Dictionary<ObjectId, int>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidRouteStructureException());

            _mockRepoManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.StationRepository)
                .Returns(_mockStationRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.SectionRepository)
                .Returns(_mockSectionRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.SyncerRepository)
                .Returns(_mockSyncerRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.TrafficLightRepository)
                .Returns(_mockTrafficLightRepo.Object);

            _mockMapper
                .Setup(x => x.Map<Route>(routeForUpdate))
                .Returns(route);

            _mockMongoClient
                .Setup(x => x.StartSessionAsync(null, default))
                .ReturnsAsync(_mockClientSession.Object);

            _mockRouteRepo
                .Setup(x => x.UpdateRouteAsync(It.IsAny<Route>(), It.IsAny<IClientSessionHandle>(), false, default))
                .ReturnsAsync(Models.Enums.UpdateResult.Modified);

            _mockSectionRepo
                .Setup(x => x.DeleteByRouteIdAsync(It.IsAny<ObjectId>(), _mockClientSession.Object, default))
                .ReturnsAsync(true);

            _mockStationRepo
                .Setup(x => x.GetCommonIdsToCountsAsync(
                    It.IsAny<IEnumerable<ObjectId>>(),
                    It.IsAny<IEnumerable<ObjectId>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<ObjectId, int>());

            _routeService = new RouteService(
                _mockAirportStateProvider.Object,
                _mockRepoManager.Object,
                _mockDomainEvents.Object,
                _mockMapper.Object,
                _mockMongoClient.Object,
                _mockRouteValidator.Object,
                _mockLogger);

            await Assert.ThrowsAsync<InvalidRouteStructureException>(
                () => _routeService.UpdateRouteAsync(
                    It.IsAny<ObjectId>(),
                    routeForUpdate,
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task UpdateRouteAsync_RouteIsNull_ThrowsArgumentNullException()
        {
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            _routeService = new RouteService(
                _mockAirportStateProvider.Object,
                _mockRepoManager.Object,
                _mockDomainEvents.Object,
                _mockMapper.Object,
                _mockMongoClient.Object,
                _mockRouteValidator.Object,
                _mockLogger);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _routeService.UpdateRouteAsync(It.IsAny<ObjectId>(), null!, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task DeleteRouteAsync_WhenCalled_ReturnsTrue()
        {
            var route = new Route();

            _mockMongoClient
                .Setup(x => x.StartSessionAsync(null, default))
                .ReturnsAsync(_mockClientSession.Object);

            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            _mockRepoManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.StationRepository)
                .Returns(_mockStationRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.SectionRepository)
                .Returns(_mockSectionRepo.Object);

            _mockRepoManager
                .SetupGet(x => x.SyncerRepository)
                .Returns(_mockSyncerRepo.Object);

            _mockRouteRepo
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(route);

            _mockRouteRepo
                .Setup(x => x.DeleteOneAsync(It.IsAny<ObjectId>(), _mockClientSession.Object, default))
                .ReturnsAsync(true);

            _mockSectionRepo
                .Setup(x => x.DeleteByRouteIdAsync(route.RouteId, _mockClientSession.Object, default))
                .ReturnsAsync(true);

            _mockStationRepo
                .Setup(x => x.GetCommonIdsToCountsAsync(It.IsAny<IEnumerable<ObjectId>>(), null, 1, default))
                .ReturnsAsync(new Dictionary<ObjectId, int>());

            _routeService = new RouteService(
                _mockAirportStateProvider.Object,
                _mockRepoManager.Object,
                _mockDomainEvents.Object,
                _mockMapper.Object,
                _mockMongoClient.Object,
                _mockRouteValidator.Object,
                _mockLogger);

            Assert.True(await _routeService.DeleteRouteAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task DeleteRouteAsync_RouteNotExists_ReturnsFalse()
        {
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            _mockRepoManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepo.Object);

            _mockRouteRepo
                .Setup(x => x.DeleteOneAsync(It.IsAny<ObjectId>(), null, default))
                .ReturnsAsync(false);

            _routeService = new RouteService(
                _mockAirportStateProvider.Object,
                _mockRepoManager.Object,
                _mockDomainEvents.Object,
                _mockMapper.Object,
                _mockMongoClient.Object,
                _mockRouteValidator.Object,
                _mockLogger);

            Assert.False(await _routeService.DeleteRouteAsync(
                It.IsAny<ObjectId>(),
                It.IsAny<CancellationToken>()));
        }
    }
}
using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Tests.Providers
{
    public class DirectionLogicProviderTests
    {
        [Fact]
        public async Task GetDirectionsByRouteIdAsync_WhenCalled_ReturnsCorrectValues()
        {
            // Arrange
            var route = new Route
            {
                RouteId = ObjectId.GenerateNewId(),
                Directions = new List<Direction>
                {
                    new Direction
                    {
                        From = ObjectId.GenerateNewId(),
                        To = ObjectId.GenerateNewId()
                    }
                }
            };
            var mockDirectionLogicFactory = new Mock<IDirectionLogicFactory>();
            var mockDirectionLogicCreator = new Mock<IDirectionLogicCreator>();
            var mockDirectionLogic = new Mock<IDirectionLogic>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockDomainEvents = new Mock<IDomainEvents>();
            var mockRepositoryManager = new Mock<IRepositoryManager>();
            var mockRouteRepo = new Mock<IRouteRepository>();
            var mockLogger = Mock.Of<ILogger<DirectionLogicProvider>>();

            mockRouteRepo
                .Setup(x => x.GetByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(route);
            mockRouteRepo
                .Setup(x => x.GetAllDirectionsAsync(default))
                .ReturnsAsync(new Dictionary<ObjectId, List<Direction>>
                {
                    { route.RouteId, route.Directions }
                });
            mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(mockRouteRepo.Object);
            mockDirectionLogicFactory
                .Setup(x => x.GetCreator(It.IsAny<Direction>()))
                .Returns(mockDirectionLogicCreator.Object);
            mockDirectionLogicCreator
                .Setup(x => x.Create())
                .Returns(mockDirectionLogic.Object);
            mockDirectionLogic
                .SetupGet(x => x.From)
                .Returns(route.Directions[0].From);
            mockDirectionLogic
                .SetupGet(x => x.To)
                .Returns(route.Directions[0].To);

            var directionLogicProvider = new DirectionLogicProvider(
                mockRepositoryManager.Object,
                mockDirectionLogicFactory.Object,
                memoryCache,
                mockDomainEvents.Object,
                mockLogger);

            // Act
            var result = await directionLogicProvider.GetByRouteIdAsync(route.RouteId);

            // Assert
            Assert.Contains(route.Directions[0].From, result.Select(d => d.From));
            Assert.Contains(route.Directions[0].To, result.Select(d => d.To));
        }

        [Fact]
        public async Task GetDirectionsByRouteIdAsync_RouteNotExist_ThrowsLogicProvisionFailedException()
        {
            // Arrange
            var mockDirectionLogicFactory = new Mock<IDirectionLogicFactory>();
            var mockRepositoryManager = new Mock<IRepositoryManager>();
            var mockRouteRepo = new Mock<IRouteRepository>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockDomainEvents = new Mock<IDomainEvents>();
            var mockLogger = Mock.Of<ILogger<DirectionLogicProvider>>();
            mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(mockRouteRepo.Object);
            mockRouteRepo
                .Setup(x => x.GetAllDirectionsAsync(default))
                .ReturnsAsync(new Dictionary<ObjectId, List<Direction>>());

            var directionLogicProvider = new DirectionLogicProvider(
                mockRepositoryManager.Object,
                mockDirectionLogicFactory.Object,
                memoryCache,
                mockDomainEvents.Object,
                mockLogger);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<LogicProvisionFailedException>(
                () => directionLogicProvider.GetByRouteIdAsync(It.IsAny<ObjectId>()));
        }
    }
}

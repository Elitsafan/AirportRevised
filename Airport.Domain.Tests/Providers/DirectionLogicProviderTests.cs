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
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockScopeFactory = new Mock<IServiceScopeFactory>();
            var mockScope = new Mock<IServiceScope>();
            var mockDirectionLogicFactory = new Mock<IDirectionLogicFactory>();
            var mockDirectionLogicCreator = new Mock<IDirectionLogicCreator>();
            var mockDirectionLogic = new Mock<IDirectionLogic>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockDomainEvents = new Mock<IDomainEvents>();
            var mockRepositoryManager = new Mock<IRepositoryManager>();
            var mockRouteRepository = new Mock<IRouteRepository>();
            var mockLogger = Mock.Of<ILogger<DirectionLogicProvider>>();

            mockRouteRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Route> { route });
            mockRouteRepository
                .Setup(x => x.GetRouteByIdAsync(
                    It.IsAny<ObjectId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(route);
            mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(mockRouteRepository.Object);
            mockScope
                .Setup(x => x.ServiceProvider)
                .Returns(mockServiceProvider.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IRepositoryManager)))
                .Returns(mockRepositoryManager.Object);
            mockServiceProvider
                .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(mockScopeFactory.Object);
            mockScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(mockScope.Object);
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
                mockServiceProvider.Object,
                mockDirectionLogicFactory.Object,
                memoryCache,
                mockDomainEvents.Object,
                mockLogger);

            // Act
            var result = await directionLogicProvider.GetDirectionsByRouteIdAsync(route.RouteId);

            // Assert
            Assert.Contains(route.Directions[0].From, result.Select(d => d.From));
            Assert.Contains(route.Directions[0].To, result.Select(d => d.To));
        }

        [Fact]
        public async Task GetDirectionsByRouteIdAsync_RouteNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockDirectionLogicFactory = new Mock<IDirectionLogicFactory>();
            var mockMemoryCache = new Mock<IMemoryCache>();
            var mockDomainEvents = new Mock<IDomainEvents>();
            var mockLogger = Mock.Of<ILogger<DirectionLogicProvider>>();
            var directionLogicProvider = new DirectionLogicProvider(
                mockServiceProvider.Object,
                mockDirectionLogicFactory.Object,
                mockMemoryCache.Object,
                mockDomainEvents.Object,
                mockLogger);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => directionLogicProvider.GetDirectionsByRouteIdAsync(It.IsAny<ObjectId>()));
        }
    }
}

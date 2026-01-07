namespace Airport.Domain.Tests.Factories
{
    public class DirectionLogicFactoryTests
    {
        #region Fields
        private Mock<IRepositoryManager> _mockRepositoryManager;
        private Mock<IRouteRepository> _mockRouteRepository; 
        #endregion

        public DirectionLogicFactoryTests()
        {
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockRouteRepository = new Mock<IRouteRepository>();

            _mockRepositoryManager
                .SetupGet(x => x.RouteRepository)
                .Returns(_mockRouteRepository.Object);
        }

        [Fact]
        public void GetCreator_WhenCalled_ReturnsNotNull()
        {
            IDirectionLogicFactory stationLogicFactory = new DirectionLogicFactory();
            var creator = stationLogicFactory.GetCreator(new Direction());

            Assert.NotNull(creator);
        }
    }
}

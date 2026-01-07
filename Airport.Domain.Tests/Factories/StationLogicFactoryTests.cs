namespace Airport.Domain.Tests.Factories
{
    public class StationLogicFactoryTests
    {
        private ServiceProvider _serviceProvider;

        public StationLogicFactoryTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILogger<StationLogic>>(Mock.Of<ILogger<StationLogic>>());
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void GetCreator_WhenCalled_ReturnsNotNull()
        {
            IStationLogicFactory stationLogicFactory = new StationLogicFactory(_serviceProvider);
            var creator = stationLogicFactory.GetCreator(new Station());

            Assert.NotNull(creator);
        }
    }
}

namespace Airport.Domain.Factories
{
    public class StationLogicFactory : IStationLogicFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public StationLogicFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IStationLogicCreator GetCreator(Station station)
        {
            if (station is null)
                throw new ArgumentNullException(nameof(station));
            var logger = _serviceProvider.GetRequiredService<ILogger<StationLogic>>();
            return new StationLogicCreator(station, logger);
        }
    }
}

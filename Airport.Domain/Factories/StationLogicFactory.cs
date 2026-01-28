namespace Airport.Domain.Factories
{
    public class StationLogicFactory : IStationLogicFactory
    {
        private readonly ILogger<StationLogic> _logger;

        public StationLogicFactory(ILogger<StationLogic> logger) => _logger = logger;

        public IStationLogicCreator GetCreator(Station station)
        {
            if (station is null)
                throw new ArgumentNullException(nameof(station));
            return new StationLogicCreator(station, _logger);
        }
    }
}

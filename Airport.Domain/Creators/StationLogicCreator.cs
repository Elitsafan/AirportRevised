namespace Airport.Domain.Creators
{
    internal class StationLogicCreator : IStationLogicCreator
    {
        private readonly Station _station;
        private readonly ILogger<IStationLogic> _logger;

        public StationLogicCreator(Station station, ILogger<IStationLogic> logger)
        {
            _station = station;
            _logger = logger;
        }

        public IStationLogic Create() => new StationLogic(_station, _logger);
    }
}

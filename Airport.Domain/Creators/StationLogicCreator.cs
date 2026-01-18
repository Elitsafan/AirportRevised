namespace Airport.Domain.Creators
{
    internal class StationLogicCreator : IStationLogicCreator
    {
        #region Fields
        private readonly Station _station;
        private readonly ILogger<StationLogic> _logger;
        #endregion

        public StationLogicCreator(Station station, ILogger<StationLogic> logger)
        {
            _station = station;
            _logger = logger;
        }

        public IStationLogic Create() => new StationLogic(_station, _logger);
    }
}

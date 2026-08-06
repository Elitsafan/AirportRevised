namespace Airport.Domain.Creators
{
    public class StationLogicCreator : IStationLogicCreator
    {
        #region Fields
        private readonly Station _station;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<StationLogic> _logger;
        #endregion

        public StationLogicCreator(Station station, IDomainEvents domainEvents, ILogger<StationLogic> logger)
        {
            _station = station;
            _domainEvents = domainEvents;
            _logger = logger;
        }

        public IStationLogic Create() => new StationLogic(_station, _domainEvents, _logger);
    }
}

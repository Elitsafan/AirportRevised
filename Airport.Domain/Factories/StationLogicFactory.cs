namespace Airport.Domain.Factories
{
    public class StationLogicFactory : IStationLogicFactory
    {
        #region Fields
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<StationLogic> _logger;
        #endregion

        public StationLogicFactory(IDomainEvents domainEvents, ILogger<StationLogic> logger)
        {
            _domainEvents = domainEvents;
            _logger = logger;
        }

        public IStationLogicCreator GetCreator(Station station)
        {
            if (station is null)
                throw new ArgumentNullException(nameof(station));

            return new StationLogicCreator(station, _domainEvents, _logger);
        }
    }
}

namespace Airport.Domain.Factories
{
    public class SectionLogicFactory : ISectionLogicFactory
    {
        #region Fields
        private readonly IStationLogicProvider _stationProvider;
        private readonly ISyncerLogicProvider _syncerProvider;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<SectionLogic> _logger;
        #endregion

        public SectionLogicFactory(
            IStationLogicProvider stationProvider,
            ISyncerLogicProvider syncerProvider,
            IDomainEvents domainEvents,
            ILogger<SectionLogic> logger)
        {
            _stationProvider = stationProvider;
            _syncerProvider = syncerProvider;
            _domainEvents = domainEvents;
            _logger = logger;
        }

        public ISectionLogicCreator GetCreator(Section section) => new SectionLogicCreator(
            section,
            _stationProvider,
            _syncerProvider,
            _domainEvents,
            _logger);
    }
}

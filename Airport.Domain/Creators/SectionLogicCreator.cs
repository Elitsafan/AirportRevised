namespace Airport.Domain.Creators
{
    internal class SectionLogicCreator : ISectionLogicCreator
    {
        #region Fields
        private readonly Section _section;
        private readonly List<ObjectId> _origin;
        private readonly List<ObjectId> _destination;
        private readonly List<ObjectId> _sectionOnly;
        private readonly IStationLogicProvider _stationProvider;
        private readonly ISyncerLogicProvider _syncerProvider;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<SectionLogic> _logger;
        #endregion

        public SectionLogicCreator(
            Section section,
            IStationLogicProvider stationProvider,
            ISyncerLogicProvider syncerProvider,
            IDomainEvents domainEvents,
            ILogger<SectionLogic> logger)
        {
            _section = section;
            _stationProvider = stationProvider;
            _syncerProvider = syncerProvider;
            _domainEvents = domainEvents;
            _logger = logger;
            _origin = section.Origin.ToList();
            _sectionOnly = section.SectionOnly.ToList();
            _destination = section.Destination.ToList();
        }

        public async Task<ISectionLogic> CreateAsync(CancellationToken ct = default)
        {
            var stations = (await _stationProvider.GetByRouteIdAsync(_section.RouteId, ct))
                .ToList();

            var origin = stations
                .Where(tl => _origin.Contains(tl.StationId))
                .ToList();

            var sectionOnly = stations
                .Where(tl => _sectionOnly.Contains(tl.StationId))
                .ToList();

            var destination = stations
                .Where(tl => _destination.Contains(tl.StationId))
                .ToList();

            var someOrigin = origin.First();
            var someDestination = destination.First();

            ISyncerLogic? syncerLogic;

            try
            {
                syncerLogic = await _syncerProvider.GetByIdAsync(_section.SyncerId, ct);
            }
            catch (LogicNotFoundException)
            {
                throw new LogicProvisionFailedException("Cannot proceed with creation.");
            }

            return new SectionLogic(
                _section,
                syncerLogic,
                _domainEvents,
                origin,
                sectionOnly,
                destination,
                _logger);
        }
    }
}

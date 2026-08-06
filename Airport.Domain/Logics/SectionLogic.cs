using Airport.Contracts.EventArgs.StationEventArgs;
using System.Collections.Concurrent;

namespace Airport.Domain.Logics
{
    public class SectionLogic : ISectionLogic
    {
        #region Fields
        private readonly ConcurrentDictionary<ObjectId, AsyncSemaphore.Releaser> _flightsTrace;
        private readonly AsyncSemaphore _trafficLightSyncer;
        private readonly Section _section;
        private readonly ISyncerLogic _syncer;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<SectionLogic> _logger;
        private readonly List<IStationLogic> _origin;
        private readonly List<IStationLogic> _destination;
        private readonly List<IStationLogic> _sectionOnly;
        private readonly HashSet<IStationLogic> _allTrafficLights;
        #endregion

        public SectionLogic(
            Section section,
            ISyncerLogic syncer,
            IDomainEvents domainEvents,
            IEnumerable<IStationLogic> origin,
            IEnumerable<IStationLogic> sectionOnly,
            IEnumerable<IStationLogic> destination,
            ILogger<SectionLogic> logger)
        {
            if (origin is null)
                throw new ArgumentNullException(nameof(origin));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));
            if (sectionOnly is null)
                throw new ArgumentNullException(nameof(sectionOnly));

            _origin = origin.ToList();
            _sectionOnly = sectionOnly.ToList();
            _destination = destination.ToList();

            if (_origin.Count == 0)
                throw new ArgumentException("Collection cannot be empty.", nameof(origin));
            if (_sectionOnly.Count == 0)
                throw new ArgumentException("Collection cannot be empty.", nameof(sectionOnly));
            if (_destination.Count == 0)
                throw new ArgumentException("Collection cannot be empty.", nameof(destination));

            _section = section;
            _syncer = syncer;
            _domainEvents = domainEvents;
            _logger = logger;
            _allTrafficLights = _origin
                .Concat(_destination)
                .ToHashSet();

            _domainEvents.StationCleared += OnExitSectionAsync;

            _flightsTrace = new();
            _trafficLightSyncer = new(1);
        }

        #region Properties
        public ObjectId SectionId => _section.SectionId;
        public ObjectId RouteId => _section.RouteId;
        public List<IStationLogic> Origin => _origin;
        public List<IStationLogic> Destination => _destination;
        public List<IStationLogic> SectionOnly => _sectionOnly;
        public HashSet<IStationLogic> TrafficLights => _allTrafficLights;
        #endregion

        public async Task EnterSectionAsync(
            IStationLogic station,
            ObjectId flightId,
            CancellationTokenSource? cts,
            CancellationToken ct = default)
        {
            if (!_origin.Contains(station))
                throw new ArgumentException("Station not found on origin.", nameof(station));

            await EnterSourceAsync(flightId, cts, ct);
        }

        protected virtual async Task OnExitSectionAsync(object? sender, IStationClearedEventArgs args)
        {
            if (RouteId != args.RouteId || Destination.All(s => s.StationId != args.CurrentStationId))
                return;

            await _syncer.ExitSectionAsync(RouteId);

            if (_flightsTrace.TryRemove(args.FlightId, out var releaser))
                releaser.Dispose();
            else
            {
                _logger.LogCritical("ERROR WHILE EXITING SECTION.");

                throw new InvalidOperationException();
            }
        }

        private async Task EnterSourceAsync(
            ObjectId flightId,
            CancellationTokenSource? cts,
            CancellationToken ct = default)
        {
            await _trafficLightSyncer.ThrowIfCancellationRequestedAsync(cts);

            var releaser = await _syncer.EnterSectionAsync(RouteId, ct);
            try
            {
                await _syncer.GetSourceRightOfWayAsync(RouteId, ct)
                    .AppendAction(() => _flightsTrace.TryAdd(flightId, releaser));
            }
            catch (OperationCanceledException)
            {
                _syncer.RollBackSourceEntrance(RouteId);

                _flightsTrace.Remove(flightId, out _);

                releaser.Dispose();

                throw;
            }
        }

        #region Common Implementations
        public void Dispose()
        {
            _domainEvents.StationCleared -= OnExitSectionAsync;
            _trafficLightSyncer?.Dispose();
        }

        public override bool Equals(object? obj) => obj is ISectionLogic section && SectionId == section.SectionId;

        public override int GetHashCode() => SectionId.GetHashCode();
        #endregion
    }
}

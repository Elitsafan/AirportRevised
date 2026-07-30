using Airport.Domain.EventArgs.StationEventArgs;

namespace Airport.Domain.Logics
{
    public class StationLogic : IStationLogic
    {
        #region Fields
        private readonly AsyncSemaphore _semaphore;
        private readonly Station _station;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<StationLogic> _logger;
        private IFlightLogic? _flightLogic;
        private AsyncSemaphore.Releaser _releaser;
        #endregion

        public StationLogic(Station station, IDomainEvents domainEvents, ILogger<StationLogic> logger)
        {
            _semaphore = new AsyncSemaphore(1);
            _station = station;
            _domainEvents = domainEvents;
            _logger = logger;
        }

        #region Properties
        public ObjectId StationId => _station.StationId;
        public FlightType? CurrentFlightType => _flightLogic?.FlightType;
        public TimeSpan EstimatedWaitingTime => _station.EstimatedWaitingTime;
        public ObjectId? CurrentFlightId => _flightLogic?.FlightId;
        #endregion

        public async Task<IStationLogic> SetFlightAsync(IFlightLogic flightLogic, CancellationTokenSource? cts)
        {
            if (flightLogic is null)
                throw new ArgumentNullException(nameof(flightLogic));

            _releaser = await _semaphore.EnterAsync(cts.GetToken());
            try
            {
                await flightLogic.ThrowIfCancellationRequestedAsync(cts);

                _flightLogic = flightLogic;

                var newFlight = _flightLogic.CurrentStation is null;

                if (!newFlight)
                    await _flightLogic.CurrentStation!.ClearAsync(StationId);

                var occupationDetails = _flightLogic.RegisterStationOccupiedDetails(StationId, DateTime.Now);

                if (newFlight)
                    await _flightLogic.RaiseFlightRunStartedAsync(StationId);
            }
            catch (Exception ex)
            {
                _flightLogic = null;
                _releaser.Dispose();

                if (ex is not OperationCanceledException)
                    _logger.LogError(ex, "FlightId: {FlightId} | StationId: {StationId}", flightLogic.FlightId, StationId);

                throw;
            }
            return this;
        }

        public async Task ClearAsync(ObjectId? newStationId, CancellationToken ct = default)
        {
            if (_flightLogic is null)
                throw new InvalidOperationException("No flight set.");

            _flightLogic.RegisterStationClearedDetails(_flightLogic.CurrentStation!.StationId, DateTime.Now);

            var routeId = _flightLogic.RouteId;
            var flightId = _flightLogic.FlightId;
            var flightType = _flightLogic.FlightType;

            _flightLogic = null;

            _releaser.Dispose();

            await RaiseStationClearedAsync(newStationId, routeId, flightId, flightType);
        }

        public void Dispose() => _semaphore?.Dispose();

        public override bool Equals(object? obj) => obj is StationLogic stationLogic &&
            StationId.Equals(stationLogic.StationId);

        public override int GetHashCode() => StationId.GetHashCode();

        protected virtual async Task RaiseStationClearedAsync(
            ObjectId? newStationId,
            ObjectId routeId,
            ObjectId flightId,
            FlightType flightType) => await _domainEvents.RaiseStationClearedAsync(new StationClearedEventArgs
            {
                CurrentStationId = newStationId,
                OldStationId = StationId,
                RouteId = routeId,
                FlightId = flightId,
                FlightType = flightType
            });
    }
}

using Airport.Domain.EventArgs.StationEventArgs;
using Airport.Domain.Helpers;
using Airport.Models.Enums;

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
                if (_flightLogic.CurrentStation is not null)
                    await _flightLogic.CurrentStation.ClearAsync();
                var occupationDetails = _flightLogic.RegisterStationOccupiedDetails(StationId, DateTime.Now);
                await RaiseStationOccupiedAsync();
            }
            catch (Exception ex)
            {
                _flightLogic = null;
                _releaser.Dispose();

                if (ex is not OperationCanceledException)
                    _logger.LogError(ex, $"{flightLogic.FlightId} | Station: {StationId}");
                throw;
            }
            return this;
        }

        public async Task ClearAsync(CancellationToken ct = default)
        {
            if (_flightLogic is null)
                throw new InvalidOperationException("No flight set.");
            var routeId = _flightLogic.RouteId;
            _flightLogic.RegisterStationClearedDetails(_flightLogic.CurrentStation!.StationId, DateTime.Now);
            await RaiseStationClearingAsync();
            var flightId = _flightLogic.FlightId;
            _flightLogic = null;
            _releaser.Dispose();
            await RaiseStationClearedAsync(routeId, flightId);
        }

        public void Dispose() => _semaphore?.Dispose();

        public override bool Equals(object? obj) => obj is StationLogic stationLogic &&
            _station.StationId.Equals(stationLogic._station.StationId);

        public override int GetHashCode() => _station.StationId.GetHashCode();

        protected virtual async Task RaiseStationOccupiedAsync() =>
            await _domainEvents.RaiseStationOccupiedAsync(
                new StationOccupiedEventArgs
                {
                    StationLogic = this,
                    FlightId = _flightLogic!.FlightId,
                    RouteId = _flightLogic.RouteId
                });

        protected virtual async Task RaiseStationClearingAsync() =>
            await _domainEvents.RaiseStationClearingAsync(
                new StationClearingEventArgs
                {
                    StationLogic = this,
                    FlightId = _flightLogic!.FlightId,
                    RouteId = _flightLogic.RouteId
                });

        protected virtual async Task RaiseStationClearedAsync(ObjectId routeId, ObjectId flightId) =>
            await _domainEvents.RaiseStationClearedAsync(
                new StationClearedEventArgs
                {
                    StationLogic = this,
                    RouteId = routeId,
                    FlightId = flightId
                });
    }
}

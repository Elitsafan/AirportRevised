using Airport.Domain.EventArgs;
using Airport.Domain.Helpers;
using Airport.Models.Enums;

namespace Airport.Domain.Logics
{
    internal class StationLogic : IStationLogic
    {
        #region Fields
        private readonly AsyncSemaphore _semaphore;
        private readonly Station _station;
        private readonly ILogger<IStationLogic> _logger;
        private IFlightLogic? _flightLogic;
        private AsyncSemaphore.Releaser _releaser;
        public event AsyncEventHandler<IStationClearingEventArgs>? StationClearingAsync;
        public event AsyncEventHandler<IStationOccupiedEventArgs>? StationOccupiedAsync;
        public event AsyncEventHandler<IStationClearedEventArgs>? StationClearedAsync;
        #endregion

        public StationLogic(Station station, ILogger<IStationLogic> logger)
        {
            _station = station;
            _logger = logger;
            _semaphore = new AsyncSemaphore(1);
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
            catch (ObjectDisposedException)
            {
                _releaser.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    _releaser.Dispose();
                else
                    _logger.LogError(ex, $"{flightLogic.FlightId} | Station: {StationId}");
                throw;
            }
            return this;
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
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

        protected virtual async Task RaiseStationOccupiedAsync()
        {
            if (StationOccupiedAsync is not null)
                await StationOccupiedAsync.InvokeAsync(this, new StationOccupiedEventArgs(_flightLogic!.FlightId));
        }

        protected virtual async Task RaiseStationClearingAsync()
        {
            if (StationClearingAsync is not null)
                await StationClearingAsync.InvokeAsync(this, new StationClearingEventArgs(_flightLogic!.FlightId));
        }

        protected virtual async Task RaiseStationClearedAsync(ObjectId routeId, ObjectId flightId)
        {
            if (StationClearedAsync is not null)
                await StationClearedAsync.InvokeAsync(this, new StationClearedEventArgs(routeId, flightId));
        }
    }
}

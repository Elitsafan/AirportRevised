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
        private IFlightLogic? _flight;
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
        public FlightType? CurrentFlightType => _flight?.FlightType;
        public TimeSpan EstimatedWaitingTime => _station.EstimatedWaitingTime;
        public ObjectId? CurrentFlightId => _flight?.FlightId;
        #endregion

        public async Task<IStationLogic> SetFlightAsync(IFlightLogic flightLogic, CancellationTokenSource? cts)
        {
            if (flightLogic is null)
                throw new ArgumentNullException(nameof(flightLogic));
            _releaser = await _semaphore.EnterAsync(cts.GetToken());
            try
            {
                await flightLogic.ThrowIfCancellationRequestedAsync(cts);
                _flight = flightLogic;
                var newFlight = _flight.CurrentStation is null;
                if (!newFlight)
                    await _flight.CurrentStation!.ClearAsync(StationId);
                var occupationDetails = _flight.RegisterStationOccupiedDetails(StationId, DateTime.Now);
                if (newFlight)
                    await _flight.RaiseFlightRunStartedAsync(StationId);
            }
            catch (Exception ex)
            {
                _flight = null;
                _releaser.Dispose();

                if (ex is not OperationCanceledException)
                    _logger.LogError(ex, $"{flightLogic.FlightId} | Station: {StationId}");
                throw;
            }
            return this;
        }

        public async Task ClearAsync(ObjectId? newStationId, CancellationToken ct = default)
        {
            if (_flight is null)
                throw new InvalidOperationException("No flight set.");
            var routeId = _flight.RouteId;
            _flight.RegisterStationClearedDetails(_flight.CurrentStation!.StationId, DateTime.Now);
            var flightId = _flight.FlightId;
            var flightType = _flight.FlightType;
            _flight = null;
            _releaser.Dispose();
            await RaiseStationClearedAsync(newStationId, routeId, flightId, flightType);
        }

        public void Dispose() => _semaphore?.Dispose();

        public override bool Equals(object? obj) => obj is StationLogic stationLogic &&
            _station.StationId.Equals(stationLogic._station.StationId);

        public override int GetHashCode() => _station.StationId.GetHashCode();

        protected virtual async Task RaiseStationClearedAsync(
            ObjectId? newStationId,
            ObjectId routeId,
            ObjectId flightId,
            FlightType flightType) => await _domainEvents.RaiseStationClearedAsync(
                new StationClearedEventArgs
                {
                    CurrentStationId = newStationId,
                    OldStationId = StationId,
                    RouteId = routeId,
                    FlightId = flightId,
                    FlightType = flightType
                });
    }
}

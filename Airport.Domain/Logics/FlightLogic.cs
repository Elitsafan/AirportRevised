using Airport.Domain.EventArgs;
using Airport.Domain.Helpers;
using Airport.Models.Enums;

namespace Airport.Domain.Logics
{
    public class FlightLogic : IFlightLogic
    {
        #region Fields
        private readonly IRouteLogic _routeLogic;
        private readonly ILogger<IFlightLogic> _logger;
        private readonly Flight _flight;
        private readonly AsyncSemaphore _syncEntrance;
        private IEnumerable<IStationLogic> _nextLeg;
        private AsyncSemaphore.Releaser _releaser;
        #endregion

        public FlightLogic(Flight flight, IRouteLogic routeLogic, ILogger<FlightLogic> logger)
        {
            _routeLogic = routeLogic;
            _logger = logger;
            _flight = flight;
            _syncEntrance = new(1);
            FlightType = flight.ToFlightType();
            RouteId = _routeLogic.RouteId;
            _nextLeg = _routeLogic
                .GetNextLeg()
                .ToArray();
            // Register a handler to each station on the first leg
            RegisterStationOccupiedAsyncEvent();
        }

        #region Properties
        public event AsyncEventHandler<IFlightRunStartedEventArgs>? FlightRunStarted;
        public event AsyncEventHandler<IFlightRunDoneEventArgs>? FlightRunDone;
        public ObjectId RouteId { get; }
        public ObjectId FlightId => _flight.FlightId;
        public IStationLogic? CurrentStation { get; private set; } = null;
        public FlightType FlightType { get; private set; }
        #endregion

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            _releaser = await _routeLogic.StartRunAsync(cancellationToken);
            try
            {
                // Gets the next leg till the end of the route
                while (_nextLeg.Any())
                {
                    CurrentStation = await _routeLogic.EnterLegAsync(this, _nextLeg, cancellationToken);
                    await Task.Delay(CurrentStation.EstimatedWaitingTime, cancellationToken);
                    _nextLeg = _routeLogic.GetNextLeg(CurrentStation);
                }
                if (CurrentStation is null)
                    throw new InvalidOperationException("Flight did not visit any station.");
                await CurrentStation!.ClearAsync(cancellationToken);
            }
            catch (LogicNotFoundException)
            {
                throw new InvalidOperationException($"Unable to keep running flight id {FlightId}");
            }
            finally { _releaser.Dispose(); }
        }

        public OccupationDetails RegisterStationOccupiedDetails(ObjectId stationId, DateTime entranceTime)
        {
            var details = new OccupationDetails
            {
                StationId = stationId,
                Entrance = entranceTime
            };
            _flight.OccupationDetails.Add(details);
            return details;
        }

        public OccupationDetails RegisterStationClearedDetails(ObjectId stationId, DateTime exitTime)
        {
            var stationOccupationDetails = _flight.OccupationDetails.First(wd => wd.StationId == stationId);
            stationOccupationDetails.Exit = exitTime;
            return stationOccupationDetails;
        }

        public async Task ThrowIfCancellationRequestedAsync(CancellationTokenSource? cts) =>
            await _syncEntrance.ThrowIfCancellationRequestedAsync(cts);

        public async Task RaiseFlightRunDoneAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FlightRunDone is not null)
                await FlightRunDone.InvokeAsync(this, new FlightRunDoneEventArgs(this));
        }

        public ValueTask DisposeAsync()
        {
            _releaser.Dispose();
            _syncEntrance.Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        // This handler invokes only the first time the StationOccupiedAsync event occured.
        private async Task OnStationOccupiedAsync(object? sender, IStationOccupiedEventArgs args)
        {
            if (args.FlightId != _flight.FlightId)
                return;
            UnregisterStationOccupiedAsyncEvent();
            _releaser.Dispose();
            if (FlightRunStarted is not null)
                await FlightRunStarted.InvokeAsync(this, new FlightRunStartedEventArgs(_flight, RouteId));
        }

        private void RegisterStationOccupiedAsyncEvent()
        {
            foreach (var station in _nextLeg)
                station.StationOccupiedAsync += OnStationOccupiedAsync;
        }

        // Unregister the handler from each station on the first leg
        private void UnregisterStationOccupiedAsyncEvent()
        {
            foreach (var station in _routeLogic.GetNextLeg())
                station.StationOccupiedAsync -= OnStationOccupiedAsync;
        }
    }
}

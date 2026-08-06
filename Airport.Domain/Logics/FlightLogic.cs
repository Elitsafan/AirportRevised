using Airport.Domain.EventArgs.FlightEventArgs;

namespace Airport.Domain.Logics
{
    public class FlightLogic : IFlightLogic
    {
        #region Fields
        private readonly IRouteLogic _routeLogic;
        private readonly ILogger<IFlightLogic> _logger;
        private readonly Flight _flight;
        private readonly AsyncSemaphore _syncEntrance;
        private readonly IDomainEvents _domainEvents;
        private List<IStationLogic> _nextLeg;
        private AsyncSemaphore.Releaser _releaser;
        #endregion

        public FlightLogic(Flight flight, IRouteLogic routeLogic, IDomainEvents domainEvents, ILogger<FlightLogic> logger)
        {
            _routeLogic = routeLogic;
            _logger = logger;
            _flight = flight;
            _syncEntrance = new(1);
            FlightType = flight.ToFlightType();
            RouteId = _routeLogic.RouteId;
            _nextLeg = _routeLogic
                .GetNextLeg()
                .ToList();
            _domainEvents = domainEvents;
        }

        #region Properties
        public ObjectId RouteId { get; }
        public ObjectId FlightId => _flight.FlightId;
        public IStationLogic? CurrentStation { get; private set; }
        public FlightType FlightType { get; private set; }
        #endregion

        public async Task RunAsync(CancellationToken ct = default)
        {
            using var routeCts = new CancellationTokenSource();

            using (_releaser = await _routeLogic.StartRunAsync(ct))
            {
                // Gets the next leg till the end of the route
                while (_nextLeg.Count > 0)
                {
                    CurrentStation = await _routeLogic.EnterLegAsync(this, _nextLeg, routeCts.Token);

                    await Task.Delay(CurrentStation.EstimatedWaitingTime, routeCts.Token);

                    _nextLeg = _routeLogic.GetNextLeg(CurrentStation).ToList();
                }

                if (CurrentStation is null)
                    throw new InvalidOperationException("Flight did not visit any station.");

                await CurrentStation!.ClearAsync(null, routeCts.Token);
            }
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

        public async Task RaiseFlightRunStartedAsync(ObjectId stationId)
        {
            _releaser.Dispose();

            await _domainEvents.RaiseFlightRunStartedAsync(new FlightRunStartedEventArgs
            {
                Flight = _flight,
                RouteId = RouteId,
                StationId = stationId
            });
        }

        public async Task RaiseFlightRunDoneAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            await _domainEvents.RaiseFlightRunDoneAsync(new FlightRunDoneEventArgs { Flight = this });
        }

        public void Dispose()
        {
            _syncEntrance.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}

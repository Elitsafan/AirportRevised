using Airport.Domain.Helpers;

namespace Airport.Domain.Logics
{
    internal class RouteLogic : IRouteLogic
    {
        #region Fields
        private Route _route = null!;
        private AsyncSemaphore _syncStartStations = null!;
        private List<IRouteSectionDetails>? _sections;
        private ILogger<RouteLogic> _logger = null!;
        private List<IStationLogic> _trafficLights = null!;
        private List<IStationLogic> _stations = null!;
        private List<IDirectionLogic> _directions = null!;
        #endregion

        public ObjectId RouteId => _route.RouteId;
        public string RouteName => _route.RouteName;

        public static async Task<RouteLogic> CreateAsync(
            Route route,
            IEnumerable<IRouteSectionDetails>? sections,
            ILogger<RouteLogic> logger,
            IDirectionLogicProvider directionLogicProvider,
            IStationLogicProvider stationLogicProvider) => await new RouteLogic().InitializeAsync(
                route,
                sections,
                logger,
                directionLogicProvider,
                stationLogicProvider);

        public async Task<AsyncSemaphore.Releaser> StartRunAsync(CancellationToken cancellationToken = default) =>
            await _syncStartStations.EnterAsync(cancellationToken);

        public async Task<IStationLogic> EnterLegAsync(
            IFlightLogic flightLogic,
            IEnumerable<IStationLogic> leg,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (leg.Except(_stations).Any())
                throw new InvalidOperationException("Not all stations belong to the route.");
            var stations = leg.ToArray();
            // An entrance to an only one station does not need a cancellation
            using var cts = stations.Length == 1 ? null : new CancellationTokenSource();
            return await StationsEntranceAttemptAsync(flightLogic, stations, cts);
        }

        public IEnumerable<IStationLogic> GetNextLeg(IStationLogic? stationLogic = null)
        {
            if (stationLogic is null)
                return _stations.ExceptBy(_route.Directions.Select(d => d.To), s => s.StationId);
            return _stations.Contains(stationLogic)
                ? _stations.Join(
                    _directions,
                    s => new { IdFrom = stationLogic.StationId, IdTo = s.StationId },
                    d => new { IdFrom = d.From, IdTo = d.To },
                    (l, r) => l)
                : throw new LogicNotFoundException("Station not found");
        }

        public override bool Equals(object? obj) => obj is RouteLogic routeLogic && _route.RouteId == routeLogic.RouteId;

        public override int GetHashCode() => _route.RouteId.GetHashCode();

        private RouteLogic()
        {
        }

        private async Task<RouteLogic> InitializeAsync(
            Route route,
            IEnumerable<IRouteSectionDetails>? sections,
            ILogger<RouteLogic> logger,
            IDirectionLogicProvider directionLogicProvider,
            IStationLogicProvider stationLogicProvider)
        {
            _route = route;
            _sections = sections?.ToList();
            _logger = logger;
            _trafficLights = new List<IStationLogic>(_sections?
                .SelectMany(s => s.RouteSection.AllTrafficLights)
                .Distinct() ?? Enumerable.Empty<IStationLogic>());
            try
            {
                _stations = new List<IStationLogic>(await stationLogicProvider.FindStationLogicsByRouteIdAsync(RouteId));
                _directions = new List<IDirectionLogic>(await directionLogicProvider.GetDirectionsByRouteIdAsync(RouteId));
            }
            catch (EntityNotFoundException)
            {
                throw new InvalidOperationException("Route not found. Cannot create route logic.");
            }
            var countStartStations = GetNextLeg().TryGetNonEnumeratedCount(out int count)
                ? count
                : GetNextLeg().Count();
            // Limits the number of flights that can enter the first stations,
            // that is, the number of flights that can start the run
            _syncStartStations = new AsyncSemaphore(countStartStations);
            return this;
        }

        private async Task<IStationLogic> StationsEntranceAttemptAsync(
            IFlightLogic flightLogic,
            IStationLogic[] stations,
            CancellationTokenSource? cts)
        {
            using var stationsCts = stations.Length == 1 ? null : new CancellationTokenSource();
            var attempts = stations
                .Select(async s => await Task.Run(
                    async () =>
                    {
                        try
                        {
                            if (_trafficLights.Contains(s))
                                await GetRightOfWayAsync(s, flightLogic.FlightId, stationsCts, cts.GetToken());
                            return await s.SetFlightAsync(flightLogic, cts);
                        }
                        catch (Exception e)
                        {
                            if (e is not OperationCanceledException && e is not ObjectDisposedException)
                                _logger.LogError(e, $"Attempt to enter station #{s.StationId}");
                            throw;
                        }
                    }, cts.GetToken()))
                .ToList();
            return await EnterStationAsync(attempts);
        }

        private async Task<IStationLogic> EnterStationAsync(List<Task<IStationLogic>> attempts)
        {
            // Filters the attempts until success
            while (attempts.Count > 0)
            {
                var enteredStation = await Task.WhenAny(attempts);
                if (enteredStation.IsCompletedSuccessfully)
                    return await enteredStation;
                // Eliminates failures
                if (enteredStation.Status == TaskStatus.Canceled || enteredStation.Status == TaskStatus.Faulted)
                    attempts.Remove(enteredStation);
            }
            throw new Exception("Couldn't enter any of the stations");
        }

        private async Task GetRightOfWayAsync(
            IStationLogic station,
            ObjectId flightId,
            CancellationTokenSource? trafficLightsCts,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IRouteSectionDetails? sourceSectionDetails = _sections!.Find(
                section => section.RouteSection.Source.Contains(station));
            if (sourceSectionDetails is not null)
                await sourceSectionDetails.EnterSectionAsync(station, flightId, trafficLightsCts, cancellationToken);
        }
    }
}
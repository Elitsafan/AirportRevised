namespace Airport.Domain.Logics
{
    public class RouteLogic : IRouteLogic
    {
        #region Fields
        private readonly AsyncSemaphore _syncStartStations;
        private readonly ILogger<RouteLogic> _logger;
        private IReadOnlyList<ISectionLogic>? _sections;
        private IReadOnlyList<IStationLogic> _trafficLights;
        private IReadOnlyList<IStationLogic> _stations;
        private IReadOnlyList<IDirectionLogic> _directions;
        //private IReadOnlyList<IStationLogic> _standaloneTrafficLights;
        #endregion

        public RouteLogic(
            Route route,
            ILogger<RouteLogic> logger,
            IEnumerable<ISectionLogic> sections,
            IEnumerable<IStationLogic> stations,
            IEnumerable<IDirectionLogic> directions,
            IEnumerable<IStationLogic> standaloneTrafficLights,
            IEnumerable<IStationLogic> sectionTrafficLights)
        {
            _logger = logger;
            _stations = stations.ToList();
            _directions = directions.ToList();
            //_standaloneTrafficLights = standaloneTrafficLights.ToList();
            _trafficLights = sectionTrafficLights.ToList();
            _sections = sections?.ToList();
            GetNextLeg().TryGetNonEnumeratedCount(out int countStartStations);
            _syncStartStations = new AsyncSemaphore(countStartStations);
            RouteId = route.RouteId;
            RouteName = route.RouteName;
        }

        #region Properties
        public ObjectId RouteId { get; }
        public string RouteName { get; }
        #endregion

        public async Task<AsyncSemaphore.Releaser> StartRunAsync(CancellationToken ct = default) =>
            await _syncStartStations.EnterAsync(ct);

        public async Task<IStationLogic> EnterLegAsync(
            IFlightLogic flightLogic,
            IEnumerable<IStationLogic> leg,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (leg.Except(_stations).Any())
                throw new InvalidOperationException("Not all stations belong to the route.");

            var stations = leg.ToArray();
            // An entrance to only one station does not need a cancellation
            using var generalCts = stations.Length == 1 ? null : new CancellationTokenSource();

            return await StationsEntranceAttemptAsync(flightLogic, stations, generalCts);
        }

        public IEnumerable<IStationLogic> GetNextLeg(IStationLogic? stationLogic = null)
        {
            if (stationLogic is null)
                return _stations
                    .ExceptBy(
                        _directions.Select(d => d.To),
                        s => s.StationId)
                    .ToList();

            return _stations.Contains(stationLogic)
                ? _stations.Join(
                    _directions,
                    s => new { IdFrom = stationLogic.StationId, IdTo = s.StationId },
                    d => new { IdFrom = d.From, IdTo = d.To },
                    (l, r) => l)
                .ToList()
                : throw new LogicNotFoundException("Station not found.");
        }

        public void Dispose() => _syncStartStations?.Dispose();

        public override bool Equals(object? obj) => obj is RouteLogic routeLogic && RouteId == routeLogic.RouteId;

        public override int GetHashCode() => RouteId.GetHashCode();

        private async Task<IStationLogic> StationsEntranceAttemptAsync(
            IFlightLogic flightLogic,
            IStationLogic[] stations,
            CancellationTokenSource? generalCts)
        {
            using var commonStationsCts = stations.Length == 1 ? null : new CancellationTokenSource();

            var attempts = stations
                .Select(async s => await Task.Run(
                    async () =>
                    {
                        try
                        {
                            if (_trafficLights.Contains(s))
                                await GetRightOfWayAsync(s, flightLogic.FlightId, commonStationsCts, generalCts.GetToken());
                            return await s.SetFlightAsync(flightLogic, generalCts);
                        }
                        catch (Exception e)
                        {
                            if (e is not OperationCanceledException && e is not ObjectDisposedException)
                                _logger.LogError(e, "Attempt to enter station #{s.StationId} failed", s.StationId);
                            throw;
                        }
                    },
                    generalCts.GetToken()))
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
                if (enteredStation.IsCanceled || enteredStation.IsFaulted)
                    attempts.Remove(enteredStation);
            }

            throw new StationEntranceFailedException("Couldn't enter any of the stations");
        }

        private async Task GetRightOfWayAsync(
            IStationLogic station,
            ObjectId flightId,
            CancellationTokenSource? trafficLightsCts,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var section = _sections!.FirstOrDefault(section => section.Origin.Contains(station));

            await (section?.EnterSectionAsync(station, flightId, trafficLightsCts, ct) ?? Task.CompletedTask);
        }
    }
}
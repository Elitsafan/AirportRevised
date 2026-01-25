using Airport.Domain.Helpers;

namespace Airport.Domain.Logics
{
    public class RouteLogic : IRouteLogic
    {
        #region Fields
        private readonly Route _route;
        private readonly AsyncSemaphore _syncStartStations;
        private readonly List<IRouteSectionDetails>? _sections;
        private readonly ILogger<RouteLogic> _logger;
        private readonly List<IStationLogic> _trafficLights;
        private readonly List<IStationLogic> _stations;
        private readonly List<IDirectionLogic> _directions;
        #endregion

        public RouteLogic(
            Route route,
            ILogger<RouteLogic> logger,
            IEnumerable<IRouteSectionDetails>? sections,
            IEnumerable<IStationLogic> stations,
            IEnumerable<IDirectionLogic> directions,
            IEnumerable<IStationLogic> trafficLights)
        {
            _route = route;
            _logger = logger;
            _stations = stations.ToList();
            _directions = directions.ToList();
            _trafficLights = trafficLights.ToList();
            _sections = sections?.ToList();
            var countStartStations = GetNextLeg().TryGetNonEnumeratedCount(out int count)
                ? count
                : GetNextLeg().Count();
            // Limits the number of flights that can enter the first stations,
            // that is, the number of flights that can start the run
            _syncStartStations = new AsyncSemaphore(countStartStations);
        }

        public ObjectId RouteId => _route.RouteId;
        public string RouteName => _route.RouteName;

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
                                _logger.LogError(e, $"Attempt to enter station #{s.StationId} failed");
                            throw;
                        }
                    },
                    cts.GetToken()))
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
            IRouteSectionDetails? sourceSectionDetails = _sections!.Find(
                section => section.RouteSection.Source.Contains(station));
            if (sourceSectionDetails is not null)
                await sourceSectionDetails.EnterSectionAsync(station, flightId, trafficLightsCts, ct);
        }
    }
}
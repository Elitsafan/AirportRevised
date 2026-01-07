using System.Collections.Concurrent;

namespace Airport.Domain.Helpers
{
    internal class RouteSectionDetails : IRouteSectionDetails
    {
        #region Fields
        private readonly ConcurrentDictionary<ObjectId, AsyncSemaphore.Releaser> _flightsTrace;
        private readonly AsyncSemaphore _trafficLightSynchronizer;
        private readonly ISectionSynchronizerDetails _synchronizer;
        #endregion

        public RouteSectionDetails(IRouteSection routeSection, ISectionSynchronizerDetails synchronizer)
        {
            RouteSection = routeSection;
            _synchronizer = synchronizer;
            foreach (var station in RouteSection.Destination)
                station.StationClearedAsync += OnExitSectionAsync;
            _flightsTrace = new();
            _trafficLightSynchronizer = new(1);
        }

        public IRouteSection RouteSection { get; } = null!;

        public async Task EnterSectionAsync(
            IStationLogic station,
            ObjectId flightId,
            CancellationTokenSource? cts,
            CancellationToken cancellationToken = default)
        {
            if (!RouteSection.Source.Contains(station))
                throw new ArgumentException("Station not found on source.", nameof(station));
            await EnterSourceAsync(flightId, cts, cancellationToken);
        }

        protected virtual async Task OnExitSectionAsync(object? sender, IStationClearedEventArgs args)
        {
            if (RouteSection.RouteId != args.RouteId)
                return;
            await _synchronizer.ExitSectionAsync(RouteSection.RouteId);
            _flightsTrace.Remove(args.FlightId, out var releaser);
            releaser.Dispose();
        }

        private async Task EnterSourceAsync(
            ObjectId flightId,
            CancellationTokenSource? cts,
            CancellationToken cancellationToken = default)
        {
            await _trafficLightSynchronizer.ThrowIfCancellationRequestedAsync(cts);
            var releaser = await _synchronizer.EnterSectionAsync(cancellationToken);
            try
            {
                await _synchronizer.GetSourceRightOfWayAsync(RouteSection.RouteId, cancellationToken)
                    .AppendAction(() => _flightsTrace.TryAdd(flightId, releaser));
            }
            catch (OperationCanceledException)
            {
                _synchronizer.RollBackSourceEntrance(RouteSection.RouteId);
                _flightsTrace.Remove(flightId, out _);
                releaser.Dispose();
                throw;
            }
        }
    }
}

using Airport.Contracts.EventArgs.StationEventArgs;
using System.Collections.Concurrent;

namespace Airport.Domain.Helpers
{
    internal class RouteSectionDetails : IRouteSectionDetails
    {
        #region Fields
        private readonly ConcurrentDictionary<ObjectId, AsyncSemaphore.Releaser> _flightsTrace;
        private readonly AsyncSemaphore _trafficLightSynchronizer;
        private readonly ISectionSynchronizerDetails _synchronizer;
        private readonly ILogger<RouteSectionDetails> _logger;
        #endregion

        public RouteSectionDetails(
            IRouteSection routeSection,
            ISectionSynchronizerDetails synchronizer,
            IDomainEvents domainEvents,
            ILogger<RouteSectionDetails> logger)
        {
            RouteSection = routeSection;
            _synchronizer = synchronizer;
            _logger = logger;
            domainEvents.StationCleared += OnExitSectionAsync;
            _flightsTrace = new();
            _trafficLightSynchronizer = new(1);
        }

        public IRouteSection RouteSection { get; }

        public async Task EnterSectionAsync(
            IStationLogic station,
            ObjectId flightId,
            CancellationTokenSource? cts,
            CancellationToken ct = default)
        {
            if (!RouteSection.Source.Contains(station))
                throw new ArgumentException("Station not found on source.", nameof(station));
            await EnterSourceAsync(flightId, cts, ct);
        }

        protected virtual async Task OnExitSectionAsync(object? sender, IStationClearedEventArgs args)
        {
            if (RouteSection.RouteId != args.RouteId)
                return;

            if (!RouteSection.Destination.Any(s => s.StationId == args.CurrentStationId))
                return;

            await _synchronizer.ExitSectionAsync(RouteSection.RouteId);
            if (_flightsTrace.TryRemove(args.FlightId, out var releaser))
                releaser.Dispose();
            else
            {
                _logger.LogCritical("ERROR WHILE EXITING SECTION.");
                throw new InvalidOperationException();
            }
        }

        private async Task EnterSourceAsync(
            ObjectId flightId,
            CancellationTokenSource? cts,
            CancellationToken ct = default)
        {
            await _trafficLightSynchronizer.ThrowIfCancellationRequestedAsync(cts);
            var releaser = await _synchronizer.EnterSectionAsync(RouteSection.RouteId, ct);
            try
            {
                await _synchronizer.GetSourceRightOfWayAsync(RouteSection.RouteId, ct)
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

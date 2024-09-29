using System.Collections.Concurrent;

namespace Airport.Domain.Helpers
{
    internal class SectionSynchronizerDetails : ISectionSynchronizerDetails
    {
        #region Fields
        private readonly AsyncSemaphore _syncCounters;
        private readonly AsyncSemaphore _syncWaiters;
        private readonly AsyncSemaphore _syncReleasers;
        private readonly AsyncSemaphore _sourceSynchronizer;
        private readonly AsyncAutoResetEvent _routeSynchronizer;
        private readonly ConcurrentDictionary<ObjectId, OccupationPair> _countOccupiedDic;
        //private readonly ConcurrentDictionary<ISet<IStationLogic>, AsyncSemaphore> _destSyncDic;
        private readonly int _capacity;
        private readonly object _syncObject;
        private int _sectionCount;
        private Task _lastWaiter = null!;
        #endregion

        public SectionSynchronizerDetails(
            IEnumerable<IRouteSection> sections,
            Dictionary<ISet<IStationLogic>, AsyncSemaphore> destinationSynchronizerDic,
            int capacity)
        {
            _routeSynchronizer = new AsyncAutoResetEvent(true);
            _syncCounters = new AsyncSemaphore(1);
            _syncWaiters = new AsyncSemaphore(1);
            _syncReleasers = new AsyncSemaphore(1);
            _sourceSynchronizer = new AsyncSemaphore(capacity);
            _countOccupiedDic = new ConcurrentDictionary<ObjectId, OccupationPair>(
                sections.Select(
                    section => new KeyValuePair<ObjectId, OccupationPair>(
                        section.RouteId,
                        new OccupationPair
                        {
                            //AllStationsCount = section.AllStationsCount,
                            CriticalOccupation = section.AllStationsCount - section.Destination.Count
                        })));
            //_destSyncDic = new(destinationSynchronizerDic);
            _sectionCount = 0;
            _capacity = capacity;
            _syncObject = new();
            _lastWaiter = Task.CompletedTask;
        }

        public async Task<AsyncSemaphore.Releaser> EnterSectionAsync(CancellationToken cancellationToken = default) =>
            await _sourceSynchronizer.EnterAsync(cancellationToken);

        public async Task GetSourceRightOfWayAsync(ObjectId routeId, CancellationToken cancellationToken = default)
        {
            var releaser = await _syncWaiters.EnterAsync(cancellationToken);
            try
            {
                await IncrementOccupiedAsync(routeId);
                if (WaitForRightOfWay(routeId))
                {
                    _lastWaiter = _routeSynchronizer.WaitAsync(cancellationToken);
                    await _lastWaiter;
                }
            }
            catch (KeyNotFoundException) { throw new InvalidOperationException("Route id not found."); }
            finally { releaser.Dispose(); }
        }

        public async Task RollBackSourceEntranceAsync(ObjectId routeId)
        {
            try
            {
                await DecrementOccupiedAsync(routeId);
            }
            catch (KeyNotFoundException) { throw new InvalidOperationException("Route id not found."); }
        }

        public async Task ExitSectionAsync(ObjectId routeId)
        {
            var releaser = await _syncReleasers.EnterAsync();
            try
            {
                if (!WaitForRightOfWay(routeId) && !_lastWaiter.IsCompleted)
                    _routeSynchronizer.Set();
                await DecrementOccupiedAsync(routeId);
            }
            catch (KeyNotFoundException) { throw new InvalidOperationException("Route id not found."); }
            finally { releaser.Dispose(); }
        }

        private async Task IncrementOccupiedAsync(ObjectId routeId)
        {
            var releaser = await _syncCounters.EnterAsync();
            try
            {
                _countOccupiedDic[routeId].CountOccupied++;
                lock (_syncObject)
                    _sectionCount++;
            }
            finally { releaser.Dispose(); }
        }

        private async Task DecrementOccupiedAsync(ObjectId routeId)
        {
            var releaser = await _syncCounters.EnterAsync();
            try
            {
                _countOccupiedDic[routeId].CountOccupied--;
                lock (_syncObject)
                    _sectionCount--;
            }
            finally { releaser.Dispose(); }
        }

        private bool WaitForRightOfWay(ObjectId routeId)
        {
            lock (_syncObject)
                return _countOccupiedDic[routeId].IsStatusCritical && _sectionCount == _capacity;
        }

        private class OccupationPair
        {
            public int CriticalOccupation { get; init; }
            public int CountOccupied { get; set; }
            //public int AllStationsCount { get; init; }
            public bool IsStatusCritical => CriticalOccupation == CountOccupied;
        }
    }
}

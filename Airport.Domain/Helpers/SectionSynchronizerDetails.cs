using System.Collections.Concurrent;

namespace Airport.Domain.Helpers
{
    internal class SectionSynchronizerDetails : ISectionSynchronizerDetails
    {
        #region Fields
        private readonly AsyncSemaphore _syncRightOfWay;
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
            _syncRightOfWay = new AsyncSemaphore(1);
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
                IncrementOccupied(routeId);
                if (WaitForRightOfWay(routeId))
                {
                    _lastWaiter = _routeSynchronizer.WaitAsync(cancellationToken);
                    await _lastWaiter;
                }
            }
            catch (KeyNotFoundException) { throw new InvalidOperationException("Route id not found."); }
            finally { releaser.Dispose(); }
        }

        public void RollBackSourceEntrance(ObjectId routeId)
        {
            try
            {
                DecrementOccupied(routeId);
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
                DecrementOccupied(routeId);
            }
            catch (KeyNotFoundException) { throw new InvalidOperationException("Route id not found."); }
            finally { releaser.Dispose(); }
        }

        private void IncrementOccupied(ObjectId routeId)
        {
            lock (_syncObject)
            {
                _countOccupiedDic[routeId].CountOccupied++;
                _sectionCount++;
            }
        }

        private void DecrementOccupied(ObjectId routeId)
        {
            lock (_syncObject)
            {
                _countOccupiedDic[routeId].CountOccupied--;
                _sectionCount--;
            }
        }

        private bool WaitForRightOfWay(ObjectId routeId)
        {
            lock (_syncObject)
            {
                return _sectionCount == _capacity &&
                    _countOccupiedDic[routeId].CriticalOccupation == _countOccupiedDic[routeId].CountOccupied - 1;
            }
        }

        private class OccupationPair
        {
            private int _countOccupied;
            private readonly object _syncObject = new();

            public int CriticalOccupation { get; init; }
            public int CountOccupied
            {
                get
                {
                    lock (_syncObject)
                        return _countOccupied;
                }
                set
                {
                    lock (_syncObject)
                        _countOccupied = value;
                }
            }
            //public int AllStationsCount { get; init; }
        }
    }
}

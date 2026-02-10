//#define TEST
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Airport.Domain.Helpers
{
    internal class SectionSynchronizerDetails : ISectionSynchronizerDetails
    {
        #region Fields
        private readonly AsyncSemaphore _syncWaiters;
        private readonly AsyncSemaphore _syncReleasers;
        private readonly AsyncSemaphore _sourceSynchronizer;
        private volatile TaskCompletionSource? _waiterTcs;
        private readonly ConcurrentDictionary<ObjectId, ISet<IStationLogic>> _routeToDest;
        private readonly ConcurrentDictionary<ObjectId, OccupationPair> _countOccupied;
        //private readonly ConcurrentDictionary<ISet<IStationLogic>, AsyncSemaphore> _destSync;
        private readonly int _capacity;
        private readonly object _syncObject;
        private int _sectionCount;
        #endregion

        public SectionSynchronizerDetails(
            IEnumerable<IRouteSection> sections,
            Dictionary<ISet<IStationLogic>, AsyncSemaphore> destSyncDic,
            int capacity)
        {
            //_routeSynchronizer = new AsyncAutoResetEvent(true);
            _syncWaiters = new AsyncSemaphore(1);
            _syncReleasers = new AsyncSemaphore(1);
            _sourceSynchronizer = new AsyncSemaphore(capacity);
            _countOccupied = new ConcurrentDictionary<ObjectId, OccupationPair>(
                sections.Select(
                    section => new KeyValuePair<ObjectId, OccupationPair>(
                        section.RouteId,
                        new OccupationPair
                        {
                            //AllStationsCount = section.AllStationsCount,
                            CriticalOccupation = section.AllStationsCount - section.Destination.Count
                        })));
            _routeToDest = new(sections
                .Select(s => new KeyValuePair<ObjectId, ISet<IStationLogic>>(
                    s.RouteId,
                    s.Destination)));
            //_destSync = new(destSyncDic, new StationLogicSetComparer());
            _sectionCount = 0;
            _capacity = capacity;
            _syncObject = new();
        }

        public async Task<AsyncSemaphore.Releaser> EnterSectionAsync(ObjectId routeId, CancellationToken ct = default) =>
            await _sourceSynchronizer.EnterAsync(ct);

        public async Task GetSourceRightOfWayAsync(ObjectId routeId, CancellationToken ct = default)
        {
            using var _ = await _syncWaiters.EnterAsync(ct);

            IncrementOccupied(routeId);
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiterTcs = tcs;
            try
            {
                if (WaitForRightOfWay(routeId))
                    using (ct.Register(() => tcs.TrySetCanceled()))
                        await tcs.Task;
            }
            finally { _waiterTcs = null; }
        }

        public void RollBackSourceEntrance(ObjectId routeId) => DecrementOccupied(routeId);

        public async Task ExitSectionAsync(ObjectId routeId)
        {
            using var _ = await _syncReleasers.EnterAsync();

            DecrementOccupied(routeId);
            if (!WaitForRightOfWay(routeId))
                _waiterTcs?.TrySetResult();
        }

        private void IncrementOccupied(ObjectId routeId)
        {
            lock (_syncObject)
            {
                _countOccupied[routeId].CountOccupied++;
                _sectionCount++;
            }
        }

        private void DecrementOccupied(ObjectId routeId)
        {
            lock (_syncObject)
            {
                _countOccupied[routeId].CountOccupied--;
                _sectionCount--;
            }
        }

        private bool WaitForRightOfWay(ObjectId routeId, [CallerMemberName] string callerName = "")
        {
            lock (_syncObject)
            {
#if TEST
                Console.WriteLine($"{callerName.PadRight(24)} | SectionCount: {_sectionCount} | Capacity: {_capacity}" +
                            $" | {_countOccupied[routeId]} | RouteId: {routeId.ToString().Last()}"); 
#endif
                return _sectionCount >= _capacity &&
                    _countOccupied[routeId].CountOccupied >= _countOccupied[routeId].CriticalOccupation;
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

#if TEST
            public override string ToString() => $"Occupied: {CountOccupied} | Critical: {CriticalOccupation}"; 
#endif
            //public int AllStationsCount { get; init; }
        }
    }
}

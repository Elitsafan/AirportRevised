//#define TEST
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Airport.Domain.Logics
{
    public class SyncerLogic : ISyncerLogic
    {
        #region Fields
        private readonly AsyncSemaphore _syncWaiters;
        private readonly AsyncSemaphore _syncReleasers;
        private readonly AsyncSemaphore _initializationSemaphore;
        private readonly ConcurrentDictionary<ObjectId, OccupationPair> _countOccupied;
        private readonly object _syncObject;
        private AsyncSemaphore _routesSyncer;
        private volatile TaskCompletionSource? _waiterTcs;
        private int _sectionsCount;
        private readonly ILogger<SyncerLogic> _logger;
        #endregion

        public SyncerLogic(Syncer syncer, ILogger<SyncerLogic> logger)
        {
            _syncWaiters = new AsyncSemaphore(1);
            _syncReleasers = new AsyncSemaphore(1);
            _initializationSemaphore = new(1);
            _countOccupied = new(syncer.SectionCriticalOccupations.Select(
                sco => new KeyValuePair<ObjectId, OccupationPair>(
                    sco.RouteId,
                    new OccupationPair
                    {
                        CountOccupied = 0,
                        CriticalOccupation = sco.Value
                    })));
            _syncObject = new();
            _sectionsCount = 0;
            SyncerId = syncer.SyncerId;
            Capacity = syncer.Capacity;
            _routesSyncer = new AsyncSemaphore(Capacity);
            _logger = logger;
        }

        #region Fields
        public int Capacity { get; private set; }
        public ObjectId SyncerId { get; }
        #endregion

        public async Task<AsyncSemaphore.Releaser> EnterSectionAsync(ObjectId routeId, CancellationToken ct = default) =>
            await _routesSyncer.EnterAsync(ct);

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

        public async Task ExitSectionAsync(ObjectId routeId)
        {
            using var _ = await _syncReleasers.EnterAsync();

            DecrementOccupied(routeId);

            if (!WaitForRightOfWay(routeId))
                _waiterTcs?.TrySetResult();
        }

        public void RollBackSourceEntrance(ObjectId routeId) => DecrementOccupied(routeId);

        public async Task UpdateAsync(
            int capacity,
            IEnumerable<SectionCriticalOccupation> occupations,
            CancellationToken ct = default)
        {
            using var _ = await _initializationSemaphore.EnterAsync(ct);

            Capacity = capacity;

            _routesSyncer.Dispose();

            _routesSyncer = new AsyncSemaphore(Capacity);

            _countOccupied.Clear();

            foreach (var occupation in occupations)
                _countOccupied[occupation.RouteId] = new OccupationPair
                {
                    CriticalOccupation = occupation.Value,
                };
        }

        public void Dispose()
        {
            _routesSyncer?.Dispose();
            _syncReleasers?.Dispose();
            _syncWaiters?.Dispose();
            _waiterTcs = null;
        }

        private void IncrementOccupied(ObjectId routeId)
        {
            lock (_syncObject)
            {
                _countOccupied[routeId].CountOccupied++;
                _sectionsCount++;
            }
        }

        private void DecrementOccupied(ObjectId routeId)
        {
            lock (_syncObject)
            {
                _countOccupied[routeId].CountOccupied--;
                _sectionsCount--;
            }
        }

        private bool WaitForRightOfWay(ObjectId routeId, [CallerMemberName] string callerName = "")
        {
            lock (_syncObject)
#if TEST
                Console.WriteLine($"{callerName.PadRight(24)} | SectionCount: {_sectionCount} | Capacity: {_capacity}" +
                            $" | {_countOccupied[routeId]} | RouteId: {routeId.ToString().Last()}"); 
#endif
                return _sectionsCount >= Capacity &&
                    _countOccupied[routeId].CountOccupied >= _countOccupied[routeId].CriticalOccupation;
        }

        #region Helpers
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
        #endregion
    }
}

using Microsoft.VisualStudio.Threading;

namespace Airport.Contracts.Logics
{
    public interface ISyncerLogic : IDisposable
    {
        ObjectId SyncerId { get; }
        int Capacity { get; }

        Task<AsyncSemaphore.Releaser> EnterSectionAsync(ObjectId routeId, CancellationToken ct = default);
        Task ExitSectionAsync(ObjectId routeId);
        /// <summary>
        /// Returns an awaitable that may be used to asynchronously acquire the next signal.
        /// </summary>
        /// <param name="routeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task GetSourceRightOfWayAsync(ObjectId routeId, CancellationToken ct = default);
        void RollBackSourceEntrance(ObjectId routeId);
        Task UpdateAsync(int capacity, IEnumerable<SectionCriticalOccupation> occupations, CancellationToken ct = default);
    }
}
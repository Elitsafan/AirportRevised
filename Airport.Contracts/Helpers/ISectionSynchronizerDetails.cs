using Microsoft.VisualStudio.Threading;
using MongoDB.Bson;

namespace Airport.Contracts.Helpers
{
    public interface ISectionSynchronizerDetails
    {
        Task<AsyncSemaphore.Releaser> EnterSectionAsync(CancellationToken ct = default);
        Task ExitSectionAsync(ObjectId routeId);
        /// <summary>
        /// Returns an awaitable that may be used to asynchronously acquire the next signal.
        /// </summary>
        /// <param name="routeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task GetSourceRightOfWayAsync(ObjectId routeId, CancellationToken ct = default);
        void RollBackSourceEntrance(ObjectId routeId);
    }
}
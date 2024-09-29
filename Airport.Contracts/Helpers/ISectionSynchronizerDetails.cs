using Microsoft.VisualStudio.Threading;
using MongoDB.Bson;

namespace Airport.Contracts.Helpers
{
    public interface ISectionSynchronizerDetails
    {
        Task<AsyncSemaphore.Releaser> EnterSectionAsync(CancellationToken cancellationToken = default);
        Task ExitSectionAsync(ObjectId routeId);
        /// <summary>
        /// Returns an awaitable that may be used to asynchronously acquire the next signal.
        /// </summary>
        /// <param name="routeId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task GetSourceRightOfWayAsync(ObjectId routeId, CancellationToken cancellationToken = default);
        Task RollBackSourceEntranceAsync(ObjectId routeId);
    }
}
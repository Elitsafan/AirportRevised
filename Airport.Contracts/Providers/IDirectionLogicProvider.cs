using Airport.Contracts.Logics;
using MongoDB.Bson;

namespace Airport.Contracts.Providers
{
    public interface IDirectionLogicProvider : IDisposable
    {
        Task<IEnumerable<IDirectionLogic>> GetByRouteIdAsync(ObjectId routeId, CancellationToken ct = default);
    }
}
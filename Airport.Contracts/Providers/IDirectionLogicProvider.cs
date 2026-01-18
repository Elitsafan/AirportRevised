using Airport.Contracts.Logics;
using MongoDB.Bson;

namespace Airport.Contracts.Providers
{
    public interface IDirectionLogicProvider : IDisposable
    {
        Task<IEnumerable<IDirectionLogic>> GetDirectionsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default);
    }
}
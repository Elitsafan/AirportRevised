using Airport.Contracts.Logics;
using MongoDB.Bson;

namespace Airport.Contracts.Providers
{
    public interface IDirectionLogicProvider
    {
        Task<IEnumerable<IDirectionLogic>> GetDirectionsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default);
    }
}
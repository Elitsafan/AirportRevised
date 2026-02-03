using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Contracts.Helpers;
using Airport.Contracts.Logics;
using Microsoft.VisualStudio.Threading;
using MongoDB.Bson;

namespace Airport.Contracts.Providers
{
    public interface IStationLogicProvider
    {
        event AsyncEventHandler<IStationStateChangedEventArgs<IStationChangedData>>? AnyStationOccupied;
        event AsyncEventHandler<IStationStateChangedEventArgs<IStationChangedData>>? AnyStationCleared;

        Task<IEnumerable<IStationLogic>> GetNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId trafficLightId,
            CancellationToken ct = default);
        Task<IEnumerable<IStationLogic>> GetByRouteIdAsync(ObjectId routeId, CancellationToken ct = default);
        Task<IEnumerable<IStationLogic>> GetTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default);
        Task<IStationLogic> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    }
}

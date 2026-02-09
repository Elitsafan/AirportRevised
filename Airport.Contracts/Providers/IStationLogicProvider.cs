using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Contracts.Helpers;
using Airport.Contracts.Logics;
using MongoDB.Bson;

namespace Airport.Contracts.Providers
{
    public interface IStationLogicProvider
    {
        Task<IEnumerable<IStationLogic>> GetNextTrafficLightsAsync(
            ObjectId routeId,
            ObjectId trafficLightId,
            CancellationToken ct = default);
        Task<IEnumerable<IStationLogic>> GetByRouteIdAsync(ObjectId routeId, CancellationToken ct = default);
        Task<IEnumerable<IStationLogic>> GetTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default);
        Task<IStationLogic> GetByIdAsync(ObjectId id, CancellationToken ct = default);
        IEnumerable<IStationChangedData> ProcessStationCleared(
            IStationClearedEventArgs args,
            CancellationToken ct = default);
        IEnumerable<IStationChangedData> ProcessFlightStarted(
            IFlightRunStartedEventArgs args,
            CancellationToken ct = default);
    }
}

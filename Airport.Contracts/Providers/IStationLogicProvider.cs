using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Contracts.Providers
{
    public interface IStationLogicProvider
    {
        Task<IEnumerable<IStationLogic>> GetByRouteIdAsync(ObjectId routeId, CancellationToken ct = default);
        Task<IEnumerable<IStationLogic>> GetTrafficLightsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default);
        Task<IEnumerable<IStationChangedData>> ProcessStationClearedAsync(
            IStationClearedEventArgs args,
            CancellationToken ct = default);
        Task<IEnumerable<IStationChangedData>> ProcessFlightStartedAsync(
            IFlightRunStartedEventArgs args,
            CancellationToken ct = default);
    }
}

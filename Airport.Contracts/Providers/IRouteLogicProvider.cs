using Airport.Contracts.Logics;
using Airport.Models.Enums;

namespace Airport.Contracts.Providers
{
    public interface IRouteLogicProvider : IDisposable
    {
        Task<IReadOnlyList<IRouteLogic>> GetDepartureRoutesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<IRouteLogic>> GetLandingRoutesAsync(CancellationToken ct = default);
        Task<IRouteLogic> GetNextRouteAsync(FlightType flightType, CancellationToken ct = default);
    }
}

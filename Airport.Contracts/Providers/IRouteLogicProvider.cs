using Airport.Contracts.Logics;
using Airport.Models.Enums;

namespace Airport.Contracts.Providers
{
    public interface IRouteLogicProvider : IDisposable
    {
        Task<IEnumerable<IRouteLogic>> GetDepartureRoutesAsync(CancellationToken ct = default);
        Task<IEnumerable<IRouteLogic>> GetLandingRoutesAsync(CancellationToken ct = default);
        Task<IRouteLogic?> GetNextRouteAsync(FlightType flightType, CancellationToken ct = default);
    }
}
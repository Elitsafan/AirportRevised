using Airport.Contracts.Logics;
using Airport.Models.Enums;

namespace Airport.Contracts.Providers
{
    public interface IRouteLogicProvider : IDisposable
    {
        Task<IEnumerable<IRouteLogic>> GetDepartureRoutesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<IRouteLogic>> GetLandingRoutesAsync(CancellationToken cancellationToken = default);
        Task<IRouteLogic?> GetNextRouteAsync(FlightType flightType, CancellationToken cancellationToken = default);
    }
}

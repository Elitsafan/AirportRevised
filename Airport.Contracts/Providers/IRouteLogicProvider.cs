using Airport.Models.Enums;

namespace Airport.Contracts.Providers
{
    public interface IRouteLogicProvider : IDisposable
    {
        Task<IRouteLogic> GetNextRouteAsync(FlightType flightType, CancellationToken ct = default);
    }
}
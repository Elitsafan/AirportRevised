using Airport.Contracts.Logics;
using Airport.Models.Enums;

namespace Airport.Contracts.Providers
{
    public interface IRouteLogicProvider
    {
        IEnumerable<IRouteLogic> LandingRoutes { get; }
        IEnumerable<IRouteLogic> DepartureRoutes { get; }
        IRouteLogic? GetNextRoute(FlightType flightType);
    }
}

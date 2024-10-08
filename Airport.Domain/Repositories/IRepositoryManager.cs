namespace Airport.Domain.Repositories
{
    public interface IRepositoryManager : System.IAsyncDisposable
    {
        IStationRepository StationRepository { get; }
        IRouteRepository RouteRepository { get; }
        IFlightRepository FlightRepository { get; }
        ITrafficLightRepository TrafficLightRepository { get; }
    }
}

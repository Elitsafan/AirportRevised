namespace Airport.Contracts.Repositories
{
    public interface IRepositoryManager : IAsyncDisposable
    {
        IStationRepository StationRepository { get; }
        IRouteRepository RouteRepository { get; }
        IFlightRepository FlightRepository { get; }
        ITrafficLightRepository TrafficLightRepository { get; }
    }
}

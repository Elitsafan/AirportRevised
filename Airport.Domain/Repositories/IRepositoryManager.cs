namespace Airport.Domain.Repositories
{
    public interface IRepositoryManager
    {
        IStationRepository StationRepository { get; }
        IRouteRepository RouteRepository { get; }
        IFlightRepository FlightRepository { get; }
        ISectionRepository SectionRepository { get; }
        ISyncerRepository SyncerRepository { get; }
        ITrafficLightRepository TrafficLightRepository { get; }
    }
}

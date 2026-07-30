using Airport.Contracts.EventArgs.RouteEventArgs;

namespace Airport.Domain.EventArgs.RouteEventArgs
{
    public class StationsByRouteUpdatedEventArgs : IStationsByRouteUpdatedEventArgs
    {
        public ObjectId RouteId { get; init; }
    }
}

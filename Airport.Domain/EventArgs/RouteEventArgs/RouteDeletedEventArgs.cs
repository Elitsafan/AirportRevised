using Airport.Contracts.EventArgs.RouteEventArgs;

namespace Airport.Domain.EventArgs.RouteEventArgs
{
    public class RouteDeletedEventArgs : IRouteDeletedEventArgs
    {
        public ObjectId RouteId { get; init; }
    }
}

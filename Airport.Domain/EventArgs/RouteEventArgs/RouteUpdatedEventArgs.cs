using Airport.Contracts.EventArgs.RouteEventArgs;

namespace Airport.Domain.EventArgs.RouteEventArgs
{
    public class RouteUpdatedEventArgs : IRouteUpdatedEventArgs
    {
        public ObjectId RouteId { get; init; }
        public List<ObjectId>? StandaloneTLIds { get; init; }
    }
}

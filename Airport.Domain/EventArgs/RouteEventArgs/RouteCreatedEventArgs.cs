using Airport.Contracts.EventArgs.RouteEventArgs;

namespace Airport.Domain.EventArgs.RouteEventArgs
{
    public class RouteCreatedEventArgs : IRouteCreatedEventArgs
    {
        public ObjectId RouteId { get; init; }
        public List<ObjectId>? StandaloneTLIds { get; init; }
    }
}

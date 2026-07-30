namespace Airport.Contracts.EventArgs.RouteEventArgs
{
    public interface IRouteCreatedEventArgs : IRouteOperationEventArgs
    {
        List<ObjectId>? StandaloneTLIds { get; }
    }
}
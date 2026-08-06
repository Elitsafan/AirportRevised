namespace Airport.Contracts.EventArgs.RouteEventArgs
{
    public interface IRouteUpdatedEventArgs : IRouteOperationEventArgs
    {
        List<ObjectId>? StandaloneTLIds { get; }
    }
}
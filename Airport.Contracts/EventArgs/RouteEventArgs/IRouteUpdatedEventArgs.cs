using Airport.Models.Entities;

namespace Airport.Contracts.EventArgs.RouteEventArgs
{
    public interface IRouteUpdatedEventArgs : IRouteOperationEventArgs
    {
        Route OldRoute { get; init; }
    }
}
using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Microsoft.VisualStudio.Threading;

namespace Airport.Contracts.Helpers
{
    public interface IDomainEvents
    {
        event Func<Task>? DataRefreshed;
        event Func<Task>? SystemResetRequested;
        event AsyncEventHandler<IFlightRunStartedEventArgs>? FlightRunStarted;
        event AsyncEventHandler<IFlightRunDoneEventArgs>? FlightRunDone;
        event AsyncEventHandler<IRouteCreatedEventArgs>? RouteCreated;
        event AsyncEventHandler<IRouteDeletedEventArgs>? RouteDeleted;
        event AsyncEventHandler<IRouteUpdatedEventArgs>? RouteUpdated;
        event AsyncEventHandler<IStationCreatedEventArgs>? StationCreated;
        event AsyncEventHandler<IStationDeletedEventArgs>? StationDeleted;
        event AsyncEventHandler<IStationUpdatedEventArgs>? StationUpdated;
        event AsyncEventHandler<IStationClearedEventArgs>? StationCleared;

        Task RaiseSystemResetRequestedAsync();
        Task RaiseDataRefreshedAsync();
        Task RaiseFlightRunStartedAsync(IFlightRunStartedEventArgs args);
        Task RaiseFlightRunDoneAsync(IFlightRunDoneEventArgs args);
        Task RaiseRouteCreatedAsync(IRouteCreatedEventArgs args);
        Task RaiseRouteUpdatedAsync(IRouteUpdatedEventArgs args);
        Task RaiseRouteDeletedAsync(IRouteDeletedEventArgs args);
        Task RaiseStationCreatedAsync(IStationCreatedEventArgs args);
        Task RaiseStationDeletedAsync(IStationDeletedEventArgs args);
        Task RaiseStationUpdatedAsync(IStationUpdatedEventArgs args);
        Task RaiseStationClearedAsync(IStationClearedEventArgs args);
    }
}

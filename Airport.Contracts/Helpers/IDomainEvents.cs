using Airport.Contracts.EventArgs.DirectionEventArgs;
using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.SectionEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Contracts.EventArgs.SyncerEventArgs;
using Microsoft.VisualStudio.Threading;

namespace Airport.Contracts.Helpers
{
    public interface IDomainEvents
    {
        event Func<Task>? DataRefreshed;
        event Func<Task>? SystemResetRequested;
        event Func<Task>? StationProviderReset;
        event Func<Task>? StationProviderRefreshed;
        event Func<Task>? DirectionProviderReset;
        event Func<Task>? DirectionProviderRefreshed;
        event Func<Task>? SectionProviderResetting;
        event Func<Task>? SectionProviderRefreshing;
        event Func<Task>? SectionProviderReset;
        event Func<Task>? SectionProviderRefreshed;
        event AsyncEventHandler<IFlightRunStartedEventArgs>? FlightRunStarted;
        event AsyncEventHandler<IFlightRunDoneEventArgs>? FlightRunDone;
        event AsyncEventHandler<IRouteCreatedEventArgs>? RouteCreated;
        event AsyncEventHandler<IRouteDeletedEventArgs>? RouteDeleted;
        event AsyncEventHandler<IRouteUpdatedEventArgs>? RouteUpdated;
        event AsyncEventHandler<IStationCreatedEventArgs>? StationCreated;
        event AsyncEventHandler<IStationDeletedEventArgs>? StationDeleted;
        event AsyncEventHandler<IStationUpdatedEventArgs>? StationUpdated;
        event AsyncEventHandler<IStationClearedEventArgs>? StationCleared;
        event AsyncEventHandler<ISectionsCreatedEventArgs>? SectionsCreated;
        event AsyncEventHandler<ISectionsDeletedEventArgs>? SectionsDeleted;
        event AsyncEventHandler<ISyncersUpdatedEventArgs>? SyncersUpdated;
        event AsyncEventHandler<ISyncersDeletedEventArgs>? SyncersDeleted;
        event AsyncEventHandler<IStationLogicUpdatedEventArgs>? StationLogicUpdated;
        event AsyncEventHandler<IStationProviderUpdatedEventArgs>? StationProviderUpdated;
        event AsyncEventHandler<IStationsByRouteUpdatedEventArgs>? StationsByRouteUpdated;
        event AsyncEventHandler<IDirectionProviderUpdatedEventArgs>? DirectionProviderUpdated;

        Task RaiseSystemResetRequestedAsync();
        Task RaiseDataRefreshedAsync();
        Task RaiseStationLogicProviderResetAsync();
        Task RaiseStationLogicProviderRefreshedAsync();
        Task RaiseDirectionLogicProviderResetAsync();
        Task RaiseDirectionLogicProviderRefreshedAsync();
        Task RaiseSectionProviderResettingAsync();
        Task RaiseSectionProviderRefreshingAsync();
        Task RaiseSectionProviderResetAsync();
        Task RaiseSectionLogicProviderRefreshedAsync();
        Task RaiseFlightRunStartedAsync(IFlightRunStartedEventArgs args);
        Task RaiseFlightRunDoneAsync(IFlightRunDoneEventArgs args);
        Task RaiseRouteCreatedAsync(IRouteCreatedEventArgs args);
        Task RaiseRouteUpdatedAsync(IRouteUpdatedEventArgs args);
        Task RaiseRouteDeletedAsync(IRouteDeletedEventArgs args);
        Task RaiseStationCreatedAsync(IStationCreatedEventArgs args);
        Task RaiseStationDeletedAsync(IStationDeletedEventArgs args);
        Task RaiseStationUpdatedAsync(IStationUpdatedEventArgs args);
        Task RaiseStationClearedAsync(IStationClearedEventArgs args);
        Task RaiseSectionsCreatedAsync(ISectionsCreatedEventArgs args);
        Task RaiseSectionsDeletedAsync(ISectionsDeletedEventArgs args);
        Task RaiseSyncersUpdatedAsync(ISyncersUpdatedEventArgs args);
        Task RaiseSyncersDeletedAsync(ISyncersDeletedEventArgs args);
        Task RaiseStationLogicUpdatedAsync(IStationLogicUpdatedEventArgs args);
        Task RaiseStationProviderUpdatedAsync(IStationProviderUpdatedEventArgs args);
        Task RaiseStationsByRouteUpdatedAsync(IStationsByRouteUpdatedEventArgs args);
        Task RaiseDirectionProviderUpdatedAsync(IDirectionProviderUpdatedEventArgs args);
    }
}

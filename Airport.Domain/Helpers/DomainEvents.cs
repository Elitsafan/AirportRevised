using Airport.Contracts.EventArgs.DirectionEventArgs;
using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.SectionEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Contracts.EventArgs.SyncerEventArgs;

namespace Airport.Domain.Helpers
{
    public class DomainEvents : IDomainEvents
    {
        #region Global Events
        public event Func<Task>? DataRefreshed;
        public event Func<Task>? SystemResetRequested;

        public async Task RaiseDataRefreshedAsync() =>
            await (DataRefreshed?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseSystemResetRequestedAsync() =>
            await (SystemResetRequested?.Invoke() ?? Task.CompletedTask);
        #endregion

        #region Flight Events
        public event AsyncEventHandler<IFlightRunStartedEventArgs>? FlightRunStarted;
        public event AsyncEventHandler<IFlightRunDoneEventArgs>? FlightRunDone;

        public async Task RaiseFlightRunStartedAsync(IFlightRunStartedEventArgs args) =>
            await (FlightRunStarted?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseFlightRunDoneAsync(IFlightRunDoneEventArgs args) =>
            await (FlightRunDone?.InvokeAsync(this, args) ?? Task.CompletedTask);
        #endregion

        #region Route Events
        public event AsyncEventHandler<IRouteCreatedEventArgs>? RouteCreated;
        public event AsyncEventHandler<IRouteDeletedEventArgs>? RouteDeleted;
        public event AsyncEventHandler<IRouteUpdatedEventArgs>? RouteUpdated;
        public event AsyncEventHandler<IStationsByRouteUpdatedEventArgs>? StationsByRouteUpdated;

        public async Task RaiseRouteCreatedAsync(IRouteCreatedEventArgs args) =>
            await (RouteCreated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseRouteUpdatedAsync(IRouteUpdatedEventArgs args) =>
            await (RouteUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseRouteDeletedAsync(IRouteDeletedEventArgs args) =>
            await (RouteDeleted?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationsByRouteUpdatedAsync(IStationsByRouteUpdatedEventArgs args) =>
            await (StationsByRouteUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);
        #endregion

        #region Direction Events
        public event Func<Task>? DirectionProviderReset;
        public event Func<Task>? DirectionProviderRefreshed;
        public event AsyncEventHandler<IDirectionProviderUpdatedEventArgs>? DirectionProviderUpdated;

        public async Task RaiseDirectionLogicProviderResetAsync() =>
            await (DirectionProviderReset?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseDirectionLogicProviderRefreshedAsync() =>
            await (DirectionProviderRefreshed?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseDirectionProviderUpdatedAsync(IDirectionProviderUpdatedEventArgs args) =>
            await (DirectionProviderUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);
        #endregion

        #region Station Events
        public event Func<Task>? StationProviderReset;
        public event Func<Task>? StationProviderRefreshed;
        public event AsyncEventHandler<IStationCreatedEventArgs>? StationCreated;
        public event AsyncEventHandler<IStationUpdatedEventArgs>? StationUpdated;
        public event AsyncEventHandler<IStationDeletedEventArgs>? StationDeleted;
        public event AsyncEventHandler<IStationClearedEventArgs>? StationCleared;
        public event AsyncEventHandler<IStationLogicUpdatedEventArgs>? StationLogicUpdated;
        public event AsyncEventHandler<IStationProviderUpdatedEventArgs>? StationProviderUpdated;

        public async Task RaiseStationLogicProviderResetAsync() =>
            await (StationProviderReset?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseStationLogicProviderRefreshedAsync() =>
            await (StationProviderRefreshed?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseStationCreatedAsync(IStationCreatedEventArgs args) =>
            await (StationCreated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationUpdatedAsync(IStationUpdatedEventArgs args) =>
            await (StationUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationLogicUpdatedAsync(IStationLogicUpdatedEventArgs args) =>
            await (StationLogicUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationProviderUpdatedAsync(IStationProviderUpdatedEventArgs args) =>
            await (StationProviderUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationDeletedAsync(IStationDeletedEventArgs args) =>
            await (StationDeleted?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationClearedAsync(IStationClearedEventArgs args) =>
            await (StationCleared?.InvokeAsync(this, args) ?? Task.CompletedTask);
        #endregion

        #region Section Events
        public event Func<Task>? SectionProviderResetting;
        public event Func<Task>? SectionProviderRefreshing;
        public event Func<Task>? SectionProviderReset;
        public event Func<Task>? SectionProviderRefreshed;
        public event AsyncEventHandler<ISectionsCreatedEventArgs>? SectionsCreated;
        public event AsyncEventHandler<ISectionsDeletedEventArgs>? SectionsDeleted;

        public async Task RaiseSectionProviderResettingAsync() =>
            await (SectionProviderResetting?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseSectionProviderRefreshingAsync() =>
            await (SectionProviderRefreshing?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseSectionProviderResetAsync() =>
            await (SectionProviderReset?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseSectionLogicProviderRefreshedAsync() =>
            await (SectionProviderRefreshed?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseSectionsCreatedAsync(ISectionsCreatedEventArgs args) =>
            await (SectionsCreated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseSectionsDeletedAsync(ISectionsDeletedEventArgs args) =>
            await (SectionsDeleted?.InvokeAsync(this, args) ?? Task.CompletedTask);
        #endregion

        #region Syncers Events
        public event AsyncEventHandler<ISyncersUpdatedEventArgs>? SyncersUpdated;
        public event AsyncEventHandler<ISyncersDeletedEventArgs>? SyncersDeleted;

        public async Task RaiseSyncersUpdatedAsync(ISyncersUpdatedEventArgs args) =>
            await (SyncersUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseSyncersDeletedAsync(ISyncersDeletedEventArgs args) =>
            await (SyncersDeleted?.InvokeAsync(this, args) ?? Task.CompletedTask);
        #endregion
    }
}

using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.Helpers
{
    public class DomainEvents : IDomainEvents
    {
        #region Global Events
        public event Func<Task>? DataRefreshed;
        public event Func<Task>? SystemResetRequested;
        #endregion

        #region Flight Events
        public event AsyncEventHandler<IFlightRunStartedEventArgs>? FlightRunStarted;
        public event AsyncEventHandler<IFlightRunDoneEventArgs>? FlightRunDone;
        #endregion

        #region Route Events
        public event AsyncEventHandler<IRouteCreatedEventArgs>? RouteCreated;
        public event AsyncEventHandler<IRouteDeletedEventArgs>? RouteDeleted;
        public event AsyncEventHandler<IRouteUpdatedEventArgs>? RouteUpdated;
        #endregion

        #region Station Events
        public event AsyncEventHandler<IStationCreatedEventArgs>? StationCreated;
        public event AsyncEventHandler<IStationUpdatedEventArgs>? StationUpdated;
        public event AsyncEventHandler<IStationDeletedEventArgs>? StationDeleted;
        public event AsyncEventHandler<IStationClearedEventArgs>? StationCleared;
        #endregion

        public async Task RaiseDataRefreshedAsync() =>
            await (DataRefreshed?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseSystemResetRequestedAsync() =>
            await (SystemResetRequested?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseFlightRunStartedAsync(IFlightRunStartedEventArgs args) =>
            await (FlightRunStarted?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseFlightRunDoneAsync(IFlightRunDoneEventArgs args) =>
            await (FlightRunDone?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseRouteCreatedAsync(IRouteCreatedEventArgs args) =>
            await (RouteCreated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseRouteUpdatedAsync(IRouteUpdatedEventArgs args) =>
            await (RouteUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseRouteDeletedAsync(IRouteDeletedEventArgs args) =>
            await (RouteDeleted?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationCreatedAsync(IStationCreatedEventArgs args) =>
            await (StationCreated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationUpdatedAsync(IStationUpdatedEventArgs args) =>
            await (StationUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationDeletedAsync(IStationDeletedEventArgs args) =>
            await (StationDeleted?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationClearedAsync(IStationClearedEventArgs args) =>
            await (StationCleared?.InvokeAsync(this, args) ?? Task.CompletedTask);
    }
}

namespace Airport.Domain.Helpers
{
    public class DomainEvents : IDomainEvents
    {
        public event AsyncEventHandler<IStationCreatedEventArgs>? StationCreated;
        public event AsyncEventHandler<IStationDeletedEventArgs>? StationDeleted;
        public event AsyncEventHandler<IStationUpdatedEventArgs>? StationUpdated;
        public event Func<Task>? DataRefreshed;
        public event Func<Task>? SystemResetRequested;

        public async Task RaiseStationCreatedAsync(IStationCreatedEventArgs args) =>
            await (StationCreated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationDeletedAsync(IStationDeletedEventArgs args) =>
            await (StationDeleted?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseStationUpdatedAsync(IStationUpdatedEventArgs args) =>
            await (StationUpdated?.InvokeAsync(this, args) ?? Task.CompletedTask);

        public async Task RaiseDataRefreshedAsync() =>
            await (DataRefreshed?.Invoke() ?? Task.CompletedTask);

        public async Task RaiseSystemResetAsync() =>
            await (SystemResetRequested?.Invoke() ?? Task.CompletedTask);
    }
}

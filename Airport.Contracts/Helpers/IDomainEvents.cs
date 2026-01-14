using Airport.Contracts.EventArgs;
using Microsoft.VisualStudio.Threading;

namespace Airport.Contracts.Helpers
{
    public interface IDomainEvents
    {
        event AsyncEventHandler<IStationDeletedEventArgs>? StationDeleted;
        event AsyncEventHandler<IStationUpdatedEventArgs>? StationUpdated;
        event AsyncEventHandler<IStationCreatedEventArgs>? StationCreated;
        event Func<Task>? DataRefreshed;
        event Func<Task>? SystemResetRequested;

        Task RaiseSystemResetAsync();
        Task RaiseStationDeletedAsync(IStationDeletedEventArgs args);
        Task RaiseStationUpdatedAsync(IStationUpdatedEventArgs args);
        Task RaiseStationCreatedAsync(IStationCreatedEventArgs args);
        Task RaiseDataRefreshedAsync();
    }
}

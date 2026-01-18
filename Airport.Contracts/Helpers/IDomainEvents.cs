using Airport.Contracts.EventArgs;
using Microsoft.VisualStudio.Threading;

namespace Airport.Contracts.Helpers
{
    public interface IDomainEvents
    {
        event AsyncEventHandler<IStationCreatedEventArgs>? StationCreated;
        event AsyncEventHandler<IStationDeletedEventArgs>? StationDeleted;
        event AsyncEventHandler<IStationUpdatedEventArgs>? StationUpdated;
        event Func<Task>? DataRefreshed;
        event Func<Task>? SystemResetRequested;

        Task RaiseSystemResetAsync();
        Task RaiseDataRefreshedAsync();
        Task RaiseStationCreatedAsync(IStationCreatedEventArgs args);
        Task RaiseStationDeletedAsync(IStationDeletedEventArgs args);
        Task RaiseStationUpdatedAsync(IStationUpdatedEventArgs args);
    }
}

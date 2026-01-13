using Airport.Contracts.EventArgs;
using Microsoft.VisualStudio.Threading;

namespace Airport.Contracts.Helpers
{
    public interface IDomainEvents
    {
        public event AsyncEventHandler<IStationDeletedEventArgs>? StationDeleted;
        public event AsyncEventHandler<IStationUpdatedEventArgs>? StationUpdated;
        public event AsyncEventHandler<IStationCreatedEventArgs>? StationCreated;
    }
}

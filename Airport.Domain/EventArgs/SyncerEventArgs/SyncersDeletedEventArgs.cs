using Airport.Contracts.EventArgs.SyncerEventArgs;

namespace Airport.Domain.EventArgs.SyncerEventArgs
{
    public class SyncersDeletedEventArgs : ISyncersDeletedEventArgs
    {
        public required List<ObjectId> SyncerIds { get; init; }
    }
}

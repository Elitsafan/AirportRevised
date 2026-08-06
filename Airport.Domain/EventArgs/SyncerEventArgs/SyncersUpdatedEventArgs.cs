using Airport.Contracts.EventArgs.SyncerEventArgs;

namespace Airport.Domain.EventArgs.SyncerEventArgs
{
    public class SyncersUpdatedEventArgs : ISyncersUpdatedEventArgs
    {
        public required List<ObjectId> SyncerIds { get; init; }
    }
}

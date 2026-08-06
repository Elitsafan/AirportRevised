namespace Airport.Contracts.EventArgs.SyncerEventArgs
{
    public interface ISyncersUpdatedEventArgs
    {
        List<ObjectId> SyncerIds { get; }
    }
}

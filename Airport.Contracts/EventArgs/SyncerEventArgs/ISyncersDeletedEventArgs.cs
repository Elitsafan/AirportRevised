namespace Airport.Contracts.EventArgs.SyncerEventArgs
{
    public interface ISyncersDeletedEventArgs
    {
        List<ObjectId> SyncerIds { get; }
    }
}

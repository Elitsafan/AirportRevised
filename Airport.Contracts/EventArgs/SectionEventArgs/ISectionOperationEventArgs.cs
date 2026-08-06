namespace Airport.Contracts.EventArgs.SectionEventArgs
{
    public interface ISectionOperationEventArgs
    {
        ObjectId RouteId { get; }
        List<ObjectId>? SectionIds { get; }
    }
}

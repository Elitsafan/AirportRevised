using Airport.Contracts.EventArgs.SectionEventArgs;

namespace Airport.Domain.EventArgs.SectionEventArgs
{
    public class SectionsDeletedEventArgs : ISectionsDeletedEventArgs
    {
        public ObjectId RouteId { get; init; }
        public List<ObjectId>? SectionIds { get; init; }
    }
}

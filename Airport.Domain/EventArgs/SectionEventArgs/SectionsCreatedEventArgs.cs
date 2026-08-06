using Airport.Contracts.EventArgs.SectionEventArgs;

namespace Airport.Domain.EventArgs.SectionEventArgs
{
    public class SectionsCreatedEventArgs : ISectionsCreatedEventArgs
    {
        public ObjectId RouteId { get; init; }
        public required List<ObjectId> SectionIds { get; init; }
    }
}

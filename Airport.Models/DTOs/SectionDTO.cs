namespace Airport.Models.DTOs
{
    public class SectionDTO<T>
        where T : IComparable<T>, IEquatable<T>
    {
        public required Guid SectionId { get; init; }
        public required HashSet<T> Origin { get; init; }
        public required HashSet<T> SectionOnly { get; init; }
        public required HashSet<T> Destination { get; init; }
    }
}

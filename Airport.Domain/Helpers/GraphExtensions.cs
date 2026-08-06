using Airport.Models.DTOs;

namespace Airport.Domain.Helpers
{
    public static class GraphExtensions
    {
        public static bool AnyDestinationOverlaps<T>(
            this IEnumerable<SectionDTO<T>> thisSections,
            IEnumerable<SectionDTO<T>> thatSections)
            where T : IComparable<T>, IEquatable<T> => thisSections.Any(
                thisSection => thatSections.Any(
                    thatSection => thisSection.Destination.Overlaps(thisSection.Destination)));
    }
}

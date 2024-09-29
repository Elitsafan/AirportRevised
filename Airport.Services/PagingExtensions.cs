using Airport.Models;

namespace Airport.Services
{
    public static class PagingExtensions
    {
        public static PagedList<T> ToPagedList<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
        {
            var count = source.TryGetNonEnumeratedCount(out int value)
                ? value
                : source.Count();
            if (pageSize * pageNumber > count && (count == 0 || pageNumber != Math.Ceiling((double)count / pageSize)))
                throw new InvalidOperationException("No such a page number for a such page size.");
            var items = source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }
}

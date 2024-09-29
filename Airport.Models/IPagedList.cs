namespace Airport.Models
{
    public interface IPagedList<T> : IEnumerable<T>
    {
        int CurrentPage { get; }
        bool HasNext { get; }
        bool HasPrevious { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
    }
}
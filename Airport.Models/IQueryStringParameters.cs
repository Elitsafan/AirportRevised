namespace Airport.Models
{
    public interface IQueryStringParameters
    {
        int PageNumber { get; set; }
        int PageSize { get; set; }
    }
}
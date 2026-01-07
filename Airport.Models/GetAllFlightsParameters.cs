namespace Airport.Models
{
    public class GetAllFlightsParameters : IQueryStringParameters
    {
        const int maxPageSize = 100;
        private int _pageSize = 50;

        public int PageNumber { get; set; } = 1;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > maxPageSize) ? maxPageSize : value;
        }
    }
}

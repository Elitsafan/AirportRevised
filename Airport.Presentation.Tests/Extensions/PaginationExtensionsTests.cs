using Airport.Models;
using Airport.Presentation.Extensions;
using Newtonsoft.Json;

namespace Airport.Presentation.Tests.Extensions
{
    public class PaginationExtensionsTests
    {
        [Fact]
        public void AddPaginationMetadata_WhenCalled_Returns_AddsCorrectPaginationAndData()
        {
            // Arrange
            var items = new List<FlightSummary>
            {
                new FlightSummary
                {
                    FlightId = ObjectId.GenerateNewId(),
                    FlightType = FlightType.Departure,
                    Stations = new List<OccupationDetails>()
                },
                new FlightSummary
                {
                    FlightId = ObjectId.GenerateNewId(),
                    FlightType = FlightType.Landing,
                    Stations = new List<OccupationDetails>()
                },
                new FlightSummary
                {
                    FlightId = ObjectId.GenerateNewId(),
                    FlightType = FlightType.Departure,
                    Stations = new List<OccupationDetails>()
                },
                new FlightSummary
                {
                    FlightId = ObjectId.GenerateNewId(),
                    FlightType = FlightType.Landing,
                    Stations = new List<OccupationDetails>()
                },
            };
            var pagedList = new PagedList<FlightSummary>(items.Take(2), 4, 1, 2);
            var summary = new SummaryWithMetadata
            {
                Summary = pagedList,
                DeparturesCount = 2,
                LandingsCount = 2
            };
            var httpCtx = new DefaultHttpContext();

            // Act
            httpCtx.Response.AddPaginationMetadata(summary);
            var json = httpCtx.Response.Headers["X-Pagination"].ToString();
            dynamic metadata = JsonConvert.DeserializeObject(json)!;

            // Assert
            Assert.Equal("X-Pagination", httpCtx.Response.Headers["Access-Control-Expose-Headers"]);
            Assert.Equal(summary.LandingsCount, (int)metadata.landingsCount);
            Assert.Equal(summary.DeparturesCount, (int)metadata.departuresCount);
            Assert.Equal(summary.Summary.TotalCount, (int)metadata.totalCount);
            Assert.Equal(summary.Summary.TotalPages, (int)metadata.totalPages);
            Assert.Equal(summary.Summary.PageSize, (int)metadata.pageSize);
            Assert.Equal(summary.Summary.CurrentPage, (int)metadata.currentPage);
        }
    }
}

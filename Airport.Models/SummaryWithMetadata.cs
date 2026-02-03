namespace Airport.Models
{
    public class SummaryWithMetadata
    {
        public required IPagedList<FlightSummary> Summary { get; init; }
        public int LandingsCount { get; init; }
        public int DeparturesCount { get; init; }
    }
}

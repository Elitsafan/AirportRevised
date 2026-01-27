using Airport.Models;

namespace Airport.Services.Abstractions
{
    public interface IAirportService : IAsyncDisposable
    {
        Task<IAirportStatus> GetStatusAsync(CancellationToken ct = default);
        Task<string> StartAsync(CancellationToken ct = default);
        Task<IPagedList<FlightSummary>> GetPagedSummaryAsync(
            GetSummaryParameters parameters,
            CancellationToken ct = default);
        Task<(int LandingsCount, int DeparturesCount)> GetFlightsCountAsync(int count, CancellationToken ct = default);
    }
}
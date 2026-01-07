using Airport.Models;

namespace Airport.Services.Abstractions
{
    public interface IAirportService : IAsyncDisposable
    {
        bool HasStarted { get; }
        Task<IAirportStatus> GetStatusAsync(CancellationToken cancellationToken = default);
        Task<string> StartAsync(CancellationToken cancellationToken = default);
        Task<IPagedList<FlightSummary>> GetPagedSummaryAsync(
            GetSummaryParameters parameters,
            CancellationToken cancellationToken = default);
        Task<(int LandingsCount, int DeparturesCount)> GetFlightsCountAsync(int count, CancellationToken cancellationToken = default);
    }
}
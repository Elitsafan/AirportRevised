using Airport.Models;

namespace Airport.Services.Abstractions
{
    public interface IAirportService : IAsyncDisposable
    {
        Task<IAirportStatus> GetStatusAsync(CancellationToken ct = default);
        Task<string> StartAsync(CancellationToken ct = default);
        Task<SummaryWithMetadata> GetSummaryWithMetadataAsync(
            GetSummaryParameters parameters,
            CancellationToken ct = default);
    }
}
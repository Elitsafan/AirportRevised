using Airport.Models;

namespace Airport.Services.Abstractions
{
    public interface IAirportService : IAsyncDisposable
    {
        Task<string> StartAsync(CancellationToken ct = default);
        Task<string> RestartAsync(CancellationToken ct = default);
        Task<IAirportStatus> GetStatusAsync(CancellationToken ct = default);
        Task<SummaryWithMetadata> GetSummaryWithMetadataAsync(
            GetSummaryParameters parameters,
            CancellationToken ct = default);
    }
}
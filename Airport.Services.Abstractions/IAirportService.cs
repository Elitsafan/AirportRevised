using Airport.Models;

namespace Airport.Services.Abstractions
{
    public interface IAirportService : IAsyncDisposable
    {
        Task<IAirportStatus> GetStatusAsync(CancellationToken cancellationToken = default);
        Task<string> StartAsync(CancellationToken cancellationToken = default);
        Task<SummaryWithMetadata> GetSummaryWithMetadataAsync(
            GetSummaryParameters parameters,
            CancellationToken ct = default);
    }
}
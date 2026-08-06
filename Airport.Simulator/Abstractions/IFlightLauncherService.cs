using Airport.Models.DTOs;
using Airport.Models.Enums;

namespace Airport.Simulator.Abstractions
{
    public interface IFlightLauncherService : IDisposable
    {
        IAsyncEnumerable<HttpResponseMessage> LaunchManyAsync(params string[]? args);
        IAsyncEnumerable<HttpResponseMessage> LaunchManyAsync(
            int n = 6,
            CancellationToken ct = default);
        Task SetFlightTimeoutAsync(FlightType? flightType = null, CancellationToken ct = default);
        Task<HttpResponseMessage> LaunchOneAsync(FlightForCreationDTO flight, CancellationToken ct = default);
        Task StartStandbyModeAsync(CancellationToken ct = default);
    }
}

using Airport.Models.DTOs;
using Airport.Models.Enums;

namespace Airport.Simulator.Abstractions
{
    public interface IFlightLauncherService : IDisposable
    {
        Task<HttpResponseMessage> StartAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<HttpResponseMessage> LaunchManyAsync(params string[]? args);
        IAsyncEnumerable<HttpResponseMessage> LaunchManyAsync(
            int n = 6,
            CancellationToken cancellationToken = default);
        Task SetFlightTimeoutAsync(FlightType? flightType = null, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> LaunchOneAsync(FlightForCreationDTO flight, CancellationToken cancellationToken = default);
    }
}

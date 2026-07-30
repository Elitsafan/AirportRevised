using Airport.Models.Entities;

namespace Airport.Services.Abstractions
{
    public interface IFlightQueue
    {
        ValueTask AddFlightAsync(Flight flight);
        IAsyncEnumerable<Flight> ReadAllAsync(CancellationToken ct = default);
    }
}

using Airport.Services.Abstractions;
using System.Threading.Channels;

namespace Airport.Services.Channels
{
    public class FlightQueue : IFlightQueue
    {
        private readonly Channel<Flight> _channel = Channel.CreateUnbounded<Flight>();

        public ValueTask AddFlightAsync(Flight flight) => _channel.Writer.WriteAsync(flight);

        public IAsyncEnumerable<Flight> ReadAllAsync(CancellationToken ct = default) =>
            _channel.Reader.ReadAllAsync(ct);
    }
}

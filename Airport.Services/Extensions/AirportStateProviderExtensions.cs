using Airport.Contracts.Providers;
using Airport.Domain.Exceptions;

namespace Airport.Services.Extensions
{
    internal static class AirportStateProviderExtensions
    {
        public static void ThrowIfNotStarted(this IAirportStateProvider airportStateProvider)
        {
            if (airportStateProvider is null)
                throw new ArgumentNullException();
            if (!airportStateProvider.HasStarted)
                throw new AirportNotStartedException("Airport needs to start/restart.");
        }
    }
}

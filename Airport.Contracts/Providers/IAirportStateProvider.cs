using Microsoft.VisualStudio.Threading;

namespace Airport.Contracts.Providers
{
    public interface IAirportStateProvider
    {
        bool HasStarted { get; set; }
        AsyncSemaphore StartLock { get; }
    }
}

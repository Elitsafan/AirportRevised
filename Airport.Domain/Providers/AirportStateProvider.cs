namespace Airport.Domain.Providers
{
    public class AirportStateProvider : IAirportStateProvider
    {
        public bool HasStarted { get; set; }
        public AsyncSemaphore StartLock { get; } = new(1);
    }
}
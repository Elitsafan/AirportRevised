namespace Airport.Simulator.Configurations
{
    public class FlightTimeoutConfiguration
    {
        public TimeSpan SendFlightTimeout { get; set; }
        public TimeSpan StandbyTimeout { get; set; }
        public int AutoFlightCount { get; set; }
        public TimeSpan KeepAliveInterval { get; set; }
    }
}
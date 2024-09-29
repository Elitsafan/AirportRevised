using Airport.Simulator.Abstractions;

namespace Airport.Simulator.Configurations
{
    internal class FlightTimeoutConfiguration : IFlightTimeoutConfiguration
    {
        public double Timeout { get; set; }
    }
}
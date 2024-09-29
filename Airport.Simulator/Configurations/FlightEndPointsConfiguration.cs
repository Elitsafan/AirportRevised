using Airport.Simulator.Abstractions;

namespace Airport.Simulator.Configurations
{
    internal class FlightEndPointsConfiguration : IFlightEndPointsConfiguration
    {
        public string BaseUrl { get; set; } = null!;
        public string Start { get; set; } = null!;
        public string Departure { get; set; } = null!;
        public string Landing { get; set; } = null!;
        public string Flights { get; set; } = null!;
    }
}

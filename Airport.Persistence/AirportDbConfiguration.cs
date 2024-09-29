using Airport.Contracts.Database;

namespace Airport.Persistence
{
    public class AirportDbConfiguration : IAirportDbConfiguration
    {
        public string DatabaseName { get; set; } = null!;
        public string ConnectionString { get; set; } = null!;
        public string FlightsCollectionName { get; set; } = null!;
        public string RoutesCollectionName { get; set; } = null!;
        public string StationsCollectionName { get; set; } = null!;
        public string TrafficLightsCollectionName { get; set; } = null!;
    }
}

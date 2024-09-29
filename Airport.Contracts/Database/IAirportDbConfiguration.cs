namespace Airport.Contracts.Database
{
    public interface IAirportDbConfiguration
    {
        string ConnectionString { get; set; }
        string FlightsCollectionName { get; set; }
        string RoutesCollectionName { get; set; }
        string StationsCollectionName { get; set; }
        string TrafficLightsCollectionName { get; set; }
        string DatabaseName { get; set; }
    }
}
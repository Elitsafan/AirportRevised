namespace Airport.Persistence
{
    public class AirportDbConfiguration
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string FlightsCollectionName { get; set; } = string.Empty;
        public string RoutesCollectionName { get; set; } = string.Empty;
        public string SectionsCollectionName { get; set; } = string.Empty;
        public string SyncersCollectionName { get; set; } = string.Empty;
        public string StationsCollectionName { get; set; } = string.Empty;
        public string TrafficLightsCollectionName { get; set; } = string.Empty;
        public TimeSpan FlushInterval { get; set; }
        public int MaxFlightDocuments { get; set; }
        public int FlightSaveBatchSize { get; set; }
    }
}

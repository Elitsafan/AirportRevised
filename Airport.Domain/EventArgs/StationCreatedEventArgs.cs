namespace Airport.Domain.EventArgs
{
    public class StationCreatedEventArgs : IStationCreatedEventArgs
    {
        public StationCreatedEventArgs(ObjectId stationId) => StationId = stationId;

        public ObjectId StationId { get; }
    }
}

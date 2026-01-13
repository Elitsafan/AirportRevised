namespace Airport.Domain.EventArgs
{
    public class StationDeletedEventArgs : IStationDeletedEventArgs
    {
        public StationDeletedEventArgs(ObjectId stationId) => StationId = stationId;

        public ObjectId StationId { get; }
    }
}

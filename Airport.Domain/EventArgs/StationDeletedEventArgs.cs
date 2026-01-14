namespace Airport.Domain.EventArgs
{
    public class StationDeletedEventArgs : System.EventArgs, IStationDeletedEventArgs
    {
        public StationDeletedEventArgs(ObjectId stationId) => StationId = stationId;

        public ObjectId StationId { get; }
    }
}

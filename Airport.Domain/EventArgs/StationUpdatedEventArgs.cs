namespace Airport.Domain.EventArgs
{
    public class StationUpdatedEventArgs : System.EventArgs, IStationUpdatedEventArgs
    {
        public StationUpdatedEventArgs(ObjectId stationId) => StationId = stationId;

        public ObjectId StationId { get; }
    }
}

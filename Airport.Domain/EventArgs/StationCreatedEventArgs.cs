namespace Airport.Domain.EventArgs
{
    public class StationCreatedEventArgs : System.EventArgs, IStationCreatedEventArgs
    {
        public StationCreatedEventArgs(ObjectId stationId) => StationId = stationId;

        public ObjectId StationId { get; }
    }
}

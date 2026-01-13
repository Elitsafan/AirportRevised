namespace Airport.Domain.EventArgs
{
    public class StationUpdatedEventArgs : IStationUpdatedEventArgs
    {
        public StationUpdatedEventArgs(ObjectId stationId) => StationId = stationId;

        public ObjectId StationId { get; }
    }
}

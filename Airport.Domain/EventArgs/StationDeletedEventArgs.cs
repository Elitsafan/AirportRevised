namespace Airport.Domain.EventArgs
{
    internal class StationDeletedEventArgs : IStationDeletedEventArgs
    {
        public StationDeletedEventArgs(ObjectId stationId) => StationId = stationId;

        public ObjectId StationId { get; }
    }
}

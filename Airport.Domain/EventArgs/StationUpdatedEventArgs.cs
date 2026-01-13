namespace Airport.Domain.EventArgs
{
    internal class StationUpdatedEventArgs : IStationUpdatedEventArgs
    {
        public StationUpdatedEventArgs(ObjectId stationId) => StationId = stationId;

        public ObjectId StationId { get; }
    }
}

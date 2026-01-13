namespace Airport.Domain.EventArgs
{
    internal class StationCreatedEventArgs : IStationCreatedEventArgs
    {
        public StationCreatedEventArgs(ObjectId stationId) => StationId = stationId;

        public ObjectId StationId { get; }
    }
}

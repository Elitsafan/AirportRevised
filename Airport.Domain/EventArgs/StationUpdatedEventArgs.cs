namespace Airport.Domain.EventArgs
{
    public class StationUpdatedEventArgs : System.EventArgs, IStationUpdatedEventArgs
    {
        public ObjectId StationId { get; init; }
    }
}

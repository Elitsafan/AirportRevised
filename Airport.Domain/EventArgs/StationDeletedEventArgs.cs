namespace Airport.Domain.EventArgs
{
    public class StationDeletedEventArgs : System.EventArgs, IStationDeletedEventArgs
    {
        public ObjectId StationId { get; init; }
    }
}
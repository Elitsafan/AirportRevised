namespace Airport.Domain.EventArgs
{
    public class StationCreatedEventArgs : System.EventArgs, IStationCreatedEventArgs
    {
        public ObjectId StationId { get; init; }
    }
}

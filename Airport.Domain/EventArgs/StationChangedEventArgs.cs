namespace Airport.Domain.EventArgs
{
    public class StationChangedEventArgs : IStationChangedEventArgs
    {
        public ObjectId StationId { get; init; }
        public IFlightInfo? Flight { get; init; }
    }
}

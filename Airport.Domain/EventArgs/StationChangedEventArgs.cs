namespace Airport.Domain.EventArgs
{
    public class StationChangedEventArgs : IStationChangedEventArgs<IStationChangedData>
    {
        public required IEnumerable<IStationChangedData> StationsState { get; init; }
    }
}
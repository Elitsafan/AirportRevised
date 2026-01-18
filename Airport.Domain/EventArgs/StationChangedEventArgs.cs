namespace Airport.Domain.EventArgs
{
    public class StationChangedEventArgs : IStationChangedEventArgs<IStationChangedData>
    {
        public required IQueryable<IStationChangedData> StationsState { get; init; }
    }
}

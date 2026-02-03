using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.EventArgs.StationEventArgs
{
    public class StationStateChangedEventArgs : IStationStateChangedEventArgs<IStationChangedData>
    {
        public required IEnumerable<IStationChangedData> StationsState { get; init; }
    }
}

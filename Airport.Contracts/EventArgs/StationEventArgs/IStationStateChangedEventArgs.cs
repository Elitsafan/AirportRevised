using Airport.Contracts.Helpers;

namespace Airport.Contracts.EventArgs.StationEventArgs
{
    public interface IStationStateChangedEventArgs<T>
        where T : IStationChangedData
    {
        IEnumerable<T> StationsState { get; init; }
    }
}

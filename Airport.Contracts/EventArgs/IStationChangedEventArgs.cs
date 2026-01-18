using Airport.Contracts.Helpers;

namespace Airport.Contracts.EventArgs
{
    public interface IStationChangedEventArgs<T>
        where T : IStationChangedData
    {
        IQueryable<T> StationsState { get; init; }
    }
}

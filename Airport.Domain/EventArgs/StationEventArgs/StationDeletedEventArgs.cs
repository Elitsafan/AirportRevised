using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.EventArgs.StationEventArgs
{
    public class StationDeletedEventArgs : IStationDeletedEventArgs
    {
        public required ObjectId StationId { get; init; }
    }
}

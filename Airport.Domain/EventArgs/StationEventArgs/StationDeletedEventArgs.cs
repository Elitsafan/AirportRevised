using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.EventArgs.StationEventArgs
{
    public class StationDeletedEventArgs : IStationDeletedEventArgs
    {
        public ObjectId StationId { get; init; }
    }
}

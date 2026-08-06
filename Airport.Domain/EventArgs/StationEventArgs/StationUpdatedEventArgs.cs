using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.EventArgs.StationEventArgs
{
    public class StationUpdatedEventArgs : IStationUpdatedEventArgs
    {
        public ObjectId StationId { get; init; }
    }
}

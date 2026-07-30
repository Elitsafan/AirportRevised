using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.EventArgs.StationEventArgs
{
    public class StationLogicUpdatedEventArgs : IStationLogicUpdatedEventArgs
    {
        public ObjectId StationId { get; init; }
    }
}

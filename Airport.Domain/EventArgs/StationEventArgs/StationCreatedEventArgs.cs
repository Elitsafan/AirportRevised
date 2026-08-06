using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.EventArgs.StationEventArgs
{
    public class StationCreatedEventArgs : IStationCreatedEventArgs
    {
        public ObjectId StationId { get; init; }
    }
}

using Airport.Contracts.EventArgs.StationEventArgs;

namespace Airport.Domain.EventArgs.StationEventArgs
{
    public class StationProviderUpdatedEventArgs : IStationProviderUpdatedEventArgs
    {
        public ObjectId StationId { get; init; }
    }
}

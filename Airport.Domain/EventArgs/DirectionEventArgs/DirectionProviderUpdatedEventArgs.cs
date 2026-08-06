using Airport.Contracts.EventArgs.DirectionEventArgs;

namespace Airport.Domain.EventArgs.DirectionEventArgs
{
    public class DirectionProviderUpdatedEventArgs : IDirectionProviderUpdatedEventArgs
    {
        public ObjectId RouteId { get; init; }
    }
}

namespace Airport.Domain.Helpers
{
    public class DomainEvents : IDomainEvents
    {
        public DomainEvents() { }

        public event AsyncEventHandler<IStationDeletedEventArgs>? StationDeleted;
        public event AsyncEventHandler<IStationUpdatedEventArgs>? StationUpdated;
        public event AsyncEventHandler<IStationCreatedEventArgs>? StationCreated;

        public void RaiseStationOperation(IStationOperationEventArgs args)
        {

        }
    }
}

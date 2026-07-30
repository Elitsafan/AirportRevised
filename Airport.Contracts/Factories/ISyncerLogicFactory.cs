namespace Airport.Contracts.Factories
{
    public interface ISyncerLogicFactory
    {
        ISyncerLogicCreator GetCreator(Syncer syncer);
    }
}

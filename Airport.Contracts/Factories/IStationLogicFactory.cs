namespace Airport.Contracts.Factories
{
    public interface IStationLogicFactory
    {
        IStationLogicCreator GetCreator(Station station);
    }
}

namespace Airport.Contracts.Factories
{
    public interface IRouteLogicFactory
    {
        IRouteLogicCreator GetCreator(Route route, IEnumerable<ISectionLogic>? sections, IEnumerable<IStationLogic>? standaloneTLs);
    }
}

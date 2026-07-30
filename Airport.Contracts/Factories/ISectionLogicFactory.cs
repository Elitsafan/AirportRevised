namespace Airport.Contracts.Factories
{
    public interface ISectionLogicFactory
    {
        ISectionLogicCreator GetCreator(Section section);
    }
}

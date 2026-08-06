namespace Airport.Contracts.Factories
{
    public interface IDirectionLogicFactory
    {
        IDirectionLogicCreator GetCreator(Direction direction);
    }
}

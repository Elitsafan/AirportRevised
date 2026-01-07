namespace Airport.Contracts.Creators
{
    public interface ILogicCreator<T>
    {
        T Create();
    }
}

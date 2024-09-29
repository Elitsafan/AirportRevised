namespace Airport.Contracts.Creators
{
    public interface ILogicCreatorAsync<T>
    {
        Task<T> CreateAsync();
    }
}
